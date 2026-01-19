library("rstudioapi")
setwd(dirname(getActiveDocumentContext()$path))

library(dplyr)
library(easystats)

library(colleyRstats)
colleyRstats::colleyRstats_setup()



### the PC with 3080 was set to German - numbers are logged 1,00 instead of 1.00
# only do once

# dir_path <- "./BenchmarkExports/3080"
#
# filesObservations <- list.files(
#   path = dir_path,
#   recursive = TRUE,
#   pattern = "*complete.csv$",
#   full.names = TRUE
# )
#
# is_int  <- function(x) grepl("^\\s*-?\\d+\\s*$", x)
# is_frac <- function(x) grepl("^\\s*\\d+\\s*$", x)
#
# fix_cols_2_to_8 <- function(line) {
#   tok <- strsplit(line, ",", fixed = TRUE)[[1]]
#   n <- length(tok)
#   if (n < 8) return(line)
#
#   out <- character(0)
#   i <- 1
#   field <- 1
#
#   # field 1
#   out <- c(out, tok[i]); i <- i + 1; field <- 2
#
#   # fields 2..8, merge d , dddd → d.dddd when adjacent tokens are numeric pieces
#   while (field <= 8 && i <= n) {
#     if (i < n && is_int(tok[i]) && is_frac(tok[i + 1])) {
#       merged <- paste0(trimws(tok[i]), ".", trimws(tok[i + 1]))
#       out <- c(out, merged)
#       i <- i + 2
#     } else {
#       out <- c(out, tok[i])
#       i <- i + 1
#     }
#     field <- field + 1
#   }
#
#   # remaining tokens untouched
#   if (i <= n) out <- c(out, tok[i:n])
#   paste(out, collapse = ",")
# }
#
# fix_trailing_three <- function(line) {
#   m <- gregexpr("\\b(True|False)\\b", line, perl = TRUE)[[1]]
#   if (m[1] == -1) return(line)
#   last_end <- m[length(m)] + attr(m, "match.length")[length(m)] - 1
#
#   prefix <- substr(line, 1, last_end)
#   suffix <- substr(line, last_end + 1, nchar(line))
#
#   # convert up to three occurrences immediately following commas: d , d+  → d.d+
#   for (k in 1:3) {
#     suffix <- sub(",\\s*(-?\\d+)\\s*,\\s*(\\d+)(?=(,|$))", ", \\1.\\2", suffix, perl = TRUE)
#   }
#   paste0(prefix, suffix)
# }
#
# fix_line <- function(line) {
#   line <- fix_cols_2_to_8(line)
#   line <- fix_trailing_three(line)
#   line
# }
#
# for (f in filesObservations) {
#   # optional backup
#   # file.copy(f, paste0(f, ".bak"), overwrite = TRUE)
#
#   lines <- readLines(f, warn = FALSE, encoding = "UTF-8")
#   lines_fixed <- vapply(lines, fix_line, character(1))
#   writeLines(lines_fixed, f, useBytes = TRUE)
# }


dir_path <- "./BenchmarkExports"

filesObservations <- list.files(
  path = dir_path,
  recursive = TRUE,
  pattern = "*complete.csv$",
  full.names = TRUE
)


main_df <- NULL
main_df <- do.call(rbind, lapply(filesObservations, function(x) {
  df <- read.delim(x, stringsAsFactors = FALSE, sep = ",", row.names = NULL, skip = 7)
  # print(x)
  df <- df[-10, ] # Remove first rows due to inaccurate data

  # Remove only extreme outliers (4*IQR)
  outliers <- performance::check_outliers(df, method = "iqr", threshold = 4)
  df <- df[!outliers, ] # Keep rows that are NOT outliers

  # print(names(df))
  # print(length(names(df)))
  # print(min(df$TotalIteration_ms))
  # print(max(df$TotalIteration_ms))
  # print(mean(df$TotalIteration_ms))
  # print(median(df$TotalIteration_ms))
  return(df)
}))


main_df$CPU <- as.factor(main_df$CPU)
main_df$GPU <- as.factor(main_df$GPU)

main_df$Segmentation_ms <- as.numeric(main_df$Segmentation_ms)
main_df$Inpainting_ms <- as.numeric(main_df$Inpainting_ms)
main_df$PostProcessing_ms <- as.numeric(main_df$PostProcessing_ms)
main_df$TotalIteration_ms <- as.numeric(main_df$TotalIteration_ms)
main_df$UnityFPS <- as.numeric(main_df$UnityFPS)
main_df$DepthEstimation_ms <- as.numeric(main_df$DepthEstimation_ms)

library(emmeans)
library(purrr)

vars <- c(
  "CPU", "GPU",
  "Segmentation_ms", "Inpainting_ms", "PostProcessing_ms", "DepthEstimation_ms",
  "TotalIteration_ms", "UnityFPS"
)
main_df2 <- stats::na.omit(main_df[, vars])

# Single factor for the combination
main_df2$Hardware <- base::interaction(main_df2$CPU, main_df2$GPU, drop = TRUE, sep = ":")


fit_one_hw <- function(dv) {
  frm <- stats::as.formula(paste0(dv, " ~ Hardware"))
  fit <- stats::lm(frm, data = main_df2)
  aov <- stats::anova(fit)
  emm <- emmeans::emmeans(fit, ~Hardware)
  pairs <- emmeans::contrast(emm, adjust = "tukey")
  list(model = fit, anova = aov, emm = emm, pairs = pairs)
}

outcomes <- c(
  "Segmentation_ms", "Inpainting_ms", "DepthEstimation_ms", "PostProcessing_ms",
  "TotalIteration_ms", "UnityFPS"
)
results_hw <- purrr::set_names(purrr::map(outcomes, fit_one_hw), outcomes)

# Examples
results_hw$TotalIteration_ms$anova
results_hw$Inpainting_ms$anova
results_hw$PostProcessing_ms$anova # not significant
results_hw$Segmentation_ms$anova
results_hw$DepthEstimation_ms$anova
results_hw$UnityFPS$anova


main_df2 |>
  group_by(Hardware) |>
  summarise(mean(TotalIteration_ms), sd(TotalIteration_ms))
main_df2 |>
  group_by(Hardware) |>
  summarise(mean(Inpainting_ms), sd(Inpainting_ms))
main_df2 |>
  group_by(Hardware) |>
  summarise(mean(PostProcessing_ms), sd(PostProcessing_ms))
main_df2 |>
  group_by(Hardware) |>
  summarise(mean(Segmentation_ms), sd(Segmentation_ms))
main_df2 |>
  group_by(Hardware) |>
  summarise(mean(DepthEstimation_ms), sd(DepthEstimation_ms))
main_df2 |>
  group_by(Hardware) |>
  summarise(mean(UnityFPS), sd(UnityFPS))


library(kableExtra)

# Outcomes to report
outcomes <- c(
  "Segmentation_ms", "Inpainting_ms", "PostProcessing_ms",
  "DepthEstimation_ms", "UnityFPS"
)

# Summaries
df_sum <- main_df2 |>
  dplyr::group_by(Hardware) |>
  dplyr::summarise(
    dplyr::across(
      dplyr::all_of(outcomes),
      list(mean = ~ mean(.x, na.rm = TRUE), sd = ~ sd(.x, na.rm = TRUE)),
      .names = "{.col}_{.fn}"
    ),
    .groups = "drop"
  ) |>
  dplyr::mutate(dplyr::across(dplyr::ends_with("_mean"), ~ base::round(.x, 2))) |>
  dplyr::mutate(dplyr::across(dplyr::ends_with("_sd"), ~ base::round(.x, 2)))

# Combine mean and sd into one column per outcome
for (col in outcomes) {
  df_sum[[col]] <- sprintf(
    "%.2f ± %.2f",
    df_sum[[paste0(col, "_mean")]],
    df_sum[[paste0(col, "_sd")]]
  )
}

# Final table, rename headers for readability
df_out <- df_sum |>
  dplyr::select(Hardware, dplyr::all_of(outcomes))

names(df_out) <- c(
  "Hardware",
  "Segmentation ms", "Inpainting ms", "Post Processing ms",
  "Depth Estimation ms", "Unity FPS"
)

# LaTeX table
latex_tab <- knitr::kable(
  df_out,
  format = "latex",
  booktabs = TRUE,
  align = c("l", rep("c", length(outcomes))),
  caption = "Mean ± SD by Hardware, two decimals",
  label = "hardware_means"
) |>
  kableExtra::kable_styling(latex_options = c("hold_position", "striped", "scale_down"))

cat(latex_tab)
