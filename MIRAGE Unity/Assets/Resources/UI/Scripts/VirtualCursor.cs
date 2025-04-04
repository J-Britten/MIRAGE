using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Virtual Cursor for navigating UI with keyboard/gamepad.
/// 
/// Mimics the input of mouse cursor
/// </summary>

[RequireComponent(typeof(RectTransform))]
public class VirtualCursor : MonoBehaviour
{
    public bool WorldSpaceCursor = false;
    public float cursorSpeed = 1000f;
    public Image cursorImage;
    public Color normalColor = Color.white;
    public Color clickedColor = Color.yellow;
    
    private RectTransform rectTransform;
    private Canvas canvas;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;
    private bool isDragging = false;
    private GameObject dragHandler = null;

    private GraphicRaycaster raycaster;   

    private float scale;

  
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        eventSystem = EventSystem.current;
        pointerEventData = new PointerEventData(eventSystem);
        raycaster = canvas.GetComponent<GraphicRaycaster>();
        scale = canvas.GetComponent<RectTransform>().localScale.x;

        // Center cursor initially
        //rectTransform.position = new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        // Hide system cursor
      //  Cursor.visible = false;
       // Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Move cursor
        Vector2 movement = new Vector2(
            Input.GetAxis("Horizontal") * cursorSpeed * Time.deltaTime * scale,
            Input.GetAxis("Vertical") * cursorSpeed * Time.deltaTime * scale
        );
        rectTransform.position += new Vector3(movement.x, movement.y, 0);
        
        // Clamp to canvas bounds using anchors
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);
        
        Vector3 pos = rectTransform.position;
        pos.x = Mathf.Clamp(pos.x, canvasCorners[0].x, canvasCorners[2].x);
        pos.y = Mathf.Clamp(pos.y, canvasCorners[0].y, canvasCorners[2].y);
        rectTransform.position = pos;
        
        // Update pointer position and process hover/drag
        if(WorldSpaceCursor) {
            pointerEventData.position = Camera.main.WorldToScreenPoint(transform.position);
            
        } else {
            pointerEventData.position = rectTransform.position;
        }

        
        
        pointerEventData.delta = movement; // Add delta for drag events
        ProcessHover();
        
        if (isDragging && dragHandler != null)
        {
            ExecuteEvents.Execute(dragHandler, pointerEventData, ExecuteEvents.dragHandler);
        }
        
        // Handle clicking
        if (Input.GetButtonDown("Submit"))
        {
            HandlePress();
            cursorImage.color = clickedColor;
        }
        else if (Input.GetButtonUp("Submit"))
        {
            HandleRelease();
            cursorImage.color = normalColor;
        }
    }

    private void ProcessHover()
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);

        if (results.Count > 0)
        {
            GameObject currentObject = results[0].gameObject;

            // Handle enter/exit events
            if (pointerEventData.pointerEnter != currentObject)
            {
                if (pointerEventData.pointerEnter != null)
                    ExecuteEvents.Execute(pointerEventData.pointerEnter, pointerEventData, ExecuteEvents.pointerExitHandler);

                GameObject newEnterTarget = ExecuteEvents.ExecuteHierarchy(currentObject, pointerEventData, ExecuteEvents.pointerEnterHandler);
                pointerEventData.pointerEnter = newEnterTarget ?? currentObject;
            }
        }
        else if (pointerEventData.pointerEnter != null)
        {
            ExecuteEvents.Execute(pointerEventData.pointerEnter, pointerEventData, ExecuteEvents.pointerExitHandler);
            pointerEventData.pointerEnter = null;
        }
    }

    private void HandlePress()
    {
        List<RaycastResult> results = new List<RaycastResult>();
        //EventSystem.current.RaycastAll(pointerEventData, results);
        raycaster.Raycast(pointerEventData, results);

        if (results.Count > 0)
        {
            GameObject hitObject = results[0].gameObject;
            pointerEventData.pressPosition = pointerEventData.position;
            pointerEventData.pointerPressRaycast = results[0];
            
            GameObject newPointerPress = ExecuteEvents.ExecuteHierarchy(hitObject, pointerEventData, ExecuteEvents.pointerDownHandler);
            if (newPointerPress == null)
                newPointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject);
            
            pointerEventData.pointerPress = newPointerPress;
            pointerEventData.rawPointerPress = hitObject;

            // Check for drag handler
            dragHandler = ExecuteEvents.GetEventHandler<IDragHandler>(hitObject);
            if (dragHandler != null)
            {
                isDragging = true;
                ExecuteEvents.Execute(dragHandler, pointerEventData, ExecuteEvents.beginDragHandler);
                pointerEventData.dragging = true;
            }
        }
    }

    private void HandleRelease()
    {
        if (isDragging && dragHandler != null)
        {
            ExecuteEvents.Execute(dragHandler, pointerEventData, ExecuteEvents.endDragHandler);
            pointerEventData.dragging = false;
            isDragging = false;
            dragHandler = null;
        }

        GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerUpHandler>(pointerEventData.pointerPress);
        if (pointerUpHandler != null)
            ExecuteEvents.Execute(pointerUpHandler, pointerEventData, ExecuteEvents.pointerUpHandler);

        GameObject pointerClickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(pointerEventData.pointerPress);
        if (pointerClickHandler != null && pointerEventData.pointerPress == pointerClickHandler)
            ExecuteEvents.Execute(pointerClickHandler, pointerEventData, ExecuteEvents.pointerClickHandler);

        pointerEventData.pressPosition = Vector2.zero;
        pointerEventData.pointerPress = null;
        pointerEventData.rawPointerPress = null;
    }

    void OnDisable()
    {
        // Restore system cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ToggleCursor(bool state)
    {
        transform.position = transform.parent.position;
        gameObject.SetActive(state);
    }
}
