library("rstudioapi")
setwd(dirname(getActiveDocumentContext()$path))

library(colleyRstats)
colleyRstats::colleyRstats_setup()

library(readr)

# Load the CSV file into a dataframe
data <- readr::read_csv2("expert-survey-mirage.csv")

# Calculate the System Usability Score (SUS)
calculate_sus <- function(data) {
  sus_columns <- c(
    "SUS[SUS001]", "SUS[SUS002]", "SUS[SUS003]", "SUS[SUS004]", "SUS[SUS005]",
    "SUS[SUS006]", "SUS[SUS007]", "SUS[SUS008]", "SUS[SUS009]", "SUS[SUS010]"
  )
  sus_data <- data[, sus_columns]

  # SUS calculation
  sus_scores <- apply(sus_data, 1, function(x) {
    odd_items <- x[c(TRUE, FALSE, TRUE, FALSE, TRUE, FALSE, TRUE, FALSE, TRUE, FALSE)]
    even_items <- x[c(FALSE, TRUE, FALSE, TRUE, FALSE, TRUE, FALSE, TRUE, FALSE, TRUE)]
    sus_score <- (sum(odd_items - 1) + sum(5 - even_items)) * 2.5
    return(sus_score)
  })

  return(sus_scores)
}

# Add SUS scores to the dataframe
data$SUS_Score <- calculate_sus(data)
mean_SUS <- mean(data$SUS_Score)
sd_SUS <- sd(data$SUS_Score)

# Calculate NASA TLX scores
calculate_tlx <- function(data) {
  tlx_columns <- c(
    "Mental[TLX001]", "Physical[TLX002]", "Temporal[TLX003]",
    "Performance[TLX004]", "Effort[TLX005]", "Frustration[TLX006]"
  )
  tlx_data <- data[, tlx_columns]

  # TLX calculation (average of the six dimensions)
  tlx_scores <- rowMeans(tlx_data, na.rm = TRUE)

  return(tlx_scores)
}

# Add TLX scores to the dataframe
data$TLX_Score <- calculate_tlx(data)
mean_TLX <- mean(data$TLX_Score)
sd_TLX <- sd(data$TLX_Score)

# Display the mean and standard deviation of SUS and TLX scores
print(mean_SUS)
print(sd_SUS)
print(mean_TLX)
print(sd_TLX)


mean_USE_Manual <- mean(data$"USEManual[USE01]")
sd_USE_Manual <- sd(data$"USEManual[USE01]")

print(mean_USE_Manual)
print(sd_USE_Manual)

mean_USE_Auto <- mean(data$"USEAuto[USE02]")
sd_USE_Auto <- sd(data$"USEAuto[USE02]")

print(mean_USE_Auto)
print(sd_USE_Auto)

mean_USE_Passenger <- mean(data$"USEPassenger[USE03]")
sd_USE_Passenger <- sd(data$"USEPassenger[USE03]")


print(mean_USE_Passenger)
print(sd_USE_Passenger)

mean_USE_Other <- mean(data$"USEOther[USE04]")
sd_USE_Other <- sd(data$"USEOther[USE04]")

print(mean_USE_Other)
print(sd_USE_Other)

# use_report <- sprintf("Participants rated whether they would see a use case for the system in different scenarios: manual driving (M = %.2f, SD = %.2f), automated driving (M = %.2f, SD = %.2f), as a passenger (M = %.2f, SD = %.2f), and in other situations like as a pedestrian or at work (M = %.2f, SD = %.2f).",
#                     mean_USE_Manual, sd_USE_Manual,
#                     mean_USE_Auto, sd_USE_Auto,
#                     mean_USE_Passenger, sd_USE_Passenger,
#                     mean_USE_Other, sd_USE_Other)
# print(use_report)

# Display the first few rows of the dataframe with SUS and TLX scores
# head(data)


# demographics
mean_AGE <- mean(data$AGE)
sd_AGE <- sd(data$AGE)

print(mean_AGE)
print(sd_AGE)


mean_experience <- mean(data$Experience)
sd_experience <- sd(data$Experience)

print(mean_experience)
print(sd_experience)

mean_publications <- mean(data$Publications)
sd_publications <- sd(data$Publications)

print(mean_publications)
print(sd_publications)

### Visualization Ratings

# Load ggplot2 library
library(ggplot2)
library(reshape2) # For data reshaping

# Create a reusable function for visualization comparisons
create_comparison_boxplot <- function(data, question_code, title, output_filename) {
  # Get all columns matching the question code
  cols <- grep(question_code, names(data), value = TRUE)

  # Reshape data from wide to long format for boxplot
  long_data <- melt(data[, cols],
    variable.name = "Visualization",
    value.name = "Rating"
  )

  # Clean up the visualization labels
  long_data$Visualization <- gsub(paste0("VisFB(.*)\\[", question_code, "\\]"), "\\1", long_data$Visualization)

  # Calculate means and standard deviations for each visualization
  viz_stats <- data.frame()
  for (viz in unique(long_data$Visualization)) {
    subset_data <- subset(long_data, Visualization == viz)
    mean_val <- mean(subset_data$Rating, na.rm = TRUE)
    sd_val <- sd(subset_data$Rating, na.rm = TRUE)
    viz_stats <- rbind(viz_stats, data.frame(Visualization = viz, Mean = mean_val, SD = sd_val))
  }

  # Create the box plot
  p <- ggplot(long_data, aes(x = Visualization, y = Rating)) +
    geom_boxplot(fill = "steelblue", alpha = 0.7) +
    stat_summary(fun = mean, geom = "point", shape = 18, size = 3, color = "red") +
    # Add mean and SD text above each box
    geom_text(
      data = viz_stats, aes(y = 7.2, label = sprintf("M = %.2f\nSD = %.2f", Mean, SD)),
      position = position_dodge(width = 0.75), size = 3.5
    ) +
    theme_minimal() +
    labs(
      title = title,
      x = "Visualization Type",
      y = "Rating (7-Point Likert)"
    ) +
    scale_y_continuous(
      limits = c(0.5, 7.5), breaks = 1:7,
      labels = c("(Strongly Disagree) 1", "2", "3", "4", "5", "6", "(Strongly Agree) 7")
    ) +
    theme(
      axis.text.x = element_text(angle = 45, hjust = 1),
      axis.text.y = element_text(size = 9)
    )

  # Display and save the plot
  show(p)
  ggsave(output_filename, p, width = 10, height = 7, units = "in", dpi = 300)

  return(p)
}

# Example usage for FB01
fb01_plot <- create_comparison_boxplot(
  data,
  "FB01",
  "From a technical point of view, this visualization worked as expected",
  "fb01_boxplot_comparison.pdf"
)

fb02_plot <- create_comparison_boxplot(
  data,
  "FB02",
  "I would use this visualization concept in my professional work (projects/publications/etc.).",
  "fb02_boxplot_comparison.pdf"
)


fb03_plot <- create_comparison_boxplot(
  data,
  "FB03",
  "I would use this visualization concept while driving manually.",
  "fb03_boxplot_comparison.pdf"
)

fb04_plot <- create_comparison_boxplot(
  data,
  "FB04",
  "I would use this visualization concept as a passenger.",
  "fb04_boxplot_comparison.pdf"
)

fb05_plot <- create_comparison_boxplot(
  data,
  "FB05",
  "I would use this visualization concept while driving in an automated vehicle.",
  "fb05_boxplot_comparison.pdf"
)


create_long_data <- function(data, question_code) {
  # Get all columns matching the question code
  cols <- grep(question_code, names(data), value = TRUE)

  # Reshape data from wide to long format for boxplot
  long_data <- melt(data[, cols],
    variable.name = "Visualization",
    value.name = "Rating"
  )

  # Clean up the visualization labels
  long_data$Visualization <- gsub(paste0("VisFB(.*)\\[", question_code, "\\]"), "\\1", long_data$Visualization)

  return(long_data)
}


long_data_FB01 <- create_long_data(data, "FB01")
long_data_FB02 <- create_long_data(data, "FB02")
long_data_FB03 <- create_long_data(data, "FB03")
long_data_FB04 <- create_long_data(data, "FB04")
long_data_FB05 <- create_long_data(data, "FB05")

ggwithinstatsWithPriorNormalityCheck(data = long_data_FB01, x = "Visualization", y = "Rating", ylab = "From a technical point of view,\n this visualization worked as expected", xlabels = c("Test" = "Test", "BBox" = "Bounding\nBox", "Mask" = "Color\nMask", "BBox" = "Bounding\nBox", "Mask" = "Color\nMask"))
ggsave("plots/as_expected.pdf", width = 12, height = 16, device = cairo_pdf)

ggwithinstatsWithPriorNormalityCheck(data = long_data_FB02, x = "Visualization", y = "Rating", ylab = "I would use this visualization concept\n in my professional work (projects/publications/etc.).", xlabels = c("Test" = "Test", "BBox" = "Bounding\nBox", "Mask" = "Color\nMask"))
ggsave("plots/prof_work.pdf", width = 12, height = 9, device = cairo_pdf)

ggwithinstatsWithPriorNormalityCheck(data = long_data_FB03, x = "Visualization", y = "Rating", ylab = "I would use this visualization concept\n while driving manually.", xlabels = c("Test" = "Test", "BBox" = "Bounding\nBox", "Mask" = "Color\nMask"))
ggsave("plots/driving_manually.pdf", width = 12, height = 9, device = cairo_pdf)

ggwithinstatsWithPriorNormalityCheck(data = long_data_FB04, x = "Visualization", y = "Rating", ylab = "I would use this visualization concept as a passenger.", xlabels = c("Test" = "Test", "BBox" = "Bounding\nBox", "Mask" = "Color\nMask"))
ggsave("plots/driving_passenger.pdf", width = 12, height = 9, device = cairo_pdf)

ggwithinstatsWithPriorNormalityCheck(data = long_data_FB05, x = "Visualization", y = "Rating", ylab = "I would use this visualization concept\n while driving in an automated vehicle.", xlabels = c("Test" = "Test", "BBox" = "Bounding\nBox", "Mask" = "Color\nMask"))
ggsave("plots/driving_automated.pdf", width = 12, height = 9, device = cairo_pdf)
