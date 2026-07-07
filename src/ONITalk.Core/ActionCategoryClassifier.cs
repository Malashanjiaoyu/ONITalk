using System;
using System.Collections.Generic;

namespace ONITalk.Core {
    public static class ActionCategoryClassifier {
        public static bool TryClassify(IEnumerable<string>? typeNames, out string category,
                out float importance) {
            if (typeNames != null) {
                foreach (string typeName in typeNames) {
                    string name = typeName ?? string.Empty;
                    if (name == "Constructable")
                        return Set("完成建造", 0.75f, out category, out importance);
                    if (name == "Repairable")
                        return Set("完成修理", 0.85f, out category, out importance);
                    if (name == "Diggable")
                        return Set("完成挖掘", 0.45f, out category, out importance);
                    if (name == "Deconstructable" || name == "Demolishable")
                        return Set("完成拆除", 0.55f, out category, out importance);
                    if (name == "Harvestable" || name.Contains("HarvestWorkable"))
                        return Set("完成收获", 0.50f, out category, out importance);
                    if (name.Contains("ComplexFabricator") ||
                            name.Contains("FoodSmoker") ||
                            name.Contains("FoodDehydrator") ||
                            name.Contains("SpiceGrinder"))
                        return Set("完成制作", 0.60f, out category, out importance);
                    if (name == "ResearchCenter" || name.Contains("ResearchCenter") ||
                            name.Contains("AnalysisStation") || name == "Studyable")
                        return Set("完成研究", 0.70f, out category, out importance);
                    if (name.Contains("Doctor") || name == "Clinic" ||
                            name.Contains("Medicinal"))
                        return Set("完成治疗", 0.90f, out category, out importance);
                    if (name.Contains("Rancher") || name == "Capturable" ||
                            name == "Butcherable")
                        return Set("完成生物照料", 0.65f, out category, out importance);
                    if (name == "Tinkerable" || name.Contains("GeoTuner"))
                        return Set("完成设备调校", 0.70f, out category, out importance);
                    if (name.Contains("Telescope") || name.Contains("Fossil") ||
                            name.Contains("DigSite"))
                        return Set("完成探索", 0.75f, out category, out importance);
                }
            }

            category = string.Empty;
            importance = 0f;
            return false;
        }

        private static bool Set(string selectedCategory, float selectedImportance,
                out string category, out float importance) {
            category = selectedCategory;
            importance = selectedImportance;
            return true;
        }
    }
}
