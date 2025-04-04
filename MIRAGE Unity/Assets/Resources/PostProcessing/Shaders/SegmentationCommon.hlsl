#ifndef SEGMENTATION_COMMON_INCLUDED
#define SEGMENTATION_COMMON_INCLUDED

struct ClassSettings {
    int classId;
    float minRange;
    float maxRange;
    float4 color;
};

StructuredBuffer<ClassSettings> selectedClasses;
int selectedClassesCount;

bool IsClassSelected(int classIndex)
{
    for (int i = 0; i < selectedClassesCount; i++)
    {
        if (selectedClasses[i].classId == classIndex)
            return true;
    }
    return false;
}

bool IsClassSelected(int classId, float depth)
{
    for (int i = 0; i < selectedClassesCount; i++)
    {
        if (selectedClasses[i].classId == classId)
        {
            if (depth >= float(selectedClasses[i].minRange) && depth <= float(selectedClasses[i].maxRange))
            {
                return true;
            }
        }
    }
    return false;
}

float4 GetColorForClassAndDepth(int classId, float depth)
{
    for (int i = 0; i < selectedClassesCount; i++)
    {
        if (selectedClasses[i].classId == classId)
        {
            if (depth >= float(selectedClasses[i].minRange) && depth <= float(selectedClasses[i].maxRange))
            {
                return selectedClasses[i].color;
            }
        }
    }
    return float4(0, 0, 0, 0);
}

#endif 