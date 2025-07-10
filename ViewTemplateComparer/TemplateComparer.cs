using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ViewTemplateComparer
{
    // Класс для хранения результата сравнения одного параметра
    public class ComparisonResult
    {
        public string GroupName { get; set; }
        public string ParameterName { get; set; }
        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public bool IsDifferent => Value1 != Value2;
    }

    // Основной класс для выполнения сравнения
    public class TemplateComparer
    {
        private readonly Document _doc;
        private readonly View _template1;
        private readonly View _template2;
        private readonly List<ComparisonResult> _results;

        public TemplateComparer(Document doc, View template1, View template2)
        {
            _doc = doc;
            _template1 = template1;
            _template2 = template2;
            _results = new List<ComparisonResult>();
        }

        public List<ComparisonResult> Compare()
        {
            CompareGeneralParameters();

            CompareCategoryOverrides("Категории модели", BuiltInParameter.VIS_GRAPHICS_MODEL, CategoryType.Model);
            CompareCategoryOverrides("Категории аннотаций", BuiltInParameter.VIS_GRAPHICS_ANNOTATION, CategoryType.Annotation);
            CompareFilters();
            CompareViewRange();

            return _results;
        }

        private bool IsParamControlled(View template, BuiltInParameter bip)
        {
            var controlledIds = template.GetTemplateParameterIds();
            var paramId = new ElementId(bip);
            return controlledIds.Contains(paramId);
        }

        private void AddResult(string group, string paramName, object val1, object val2)
        {
            _results.Add(new ComparisonResult
            {
                GroupName = group,
                ParameterName = paramName,
                Value1 = val1?.ToString() ?? "null",
                Value2 = val2?.ToString() ?? "null"
            });
        }

        private string GetValueOrNotControlled(View template, BuiltInParameter bip)
        {
            if (!IsParamControlled(template, bip))
                return "Не управляется";

            var param = template.get_Parameter(bip);
            if (param == null || !param.HasValue) return "Нет значения";

            switch (param.StorageType)
            {
                case StorageType.ElementId:
                    var id = param.AsElementId();
                    if (id == ElementId.InvalidElementId) return "Нет";
                    var elem = _doc.GetElement(id);
                    return elem?.Name ?? id.ToString();
                case StorageType.String:
                    return param.AsString();
                default:
                    return param.AsValueString() ?? param.AsString() ?? "N/A";
            }
        }

        private bool AreOverridesEqual(OverrideGraphicSettings ov1, OverrideGraphicSettings ov2)
        {
            if (ov1 == null || ov2 == null) return ov1 == ov2;

            return ov1.Halftone == ov2.Halftone &&
                   ov1.Transparency == ov2.Transparency &&
                   ov1.DetailLevel == ov2.DetailLevel &&
                   ov1.ProjectionLineWeight == ov2.ProjectionLineWeight &&
                   ov1.ProjectionLinePatternId == ov2.ProjectionLinePatternId &&
                   ov1.ProjectionLineColor.Equals(ov2.ProjectionLineColor);
        }

        private void CompareGeneralParameters()
        {
            const string group = "Основные параметры";
            AddResult(group, "Масштаб вида", GetValueOrNotControlled(_template1, BuiltInParameter.VIEW_SCALE), GetValueOrNotControlled(_template2, BuiltInParameter.VIEW_SCALE));
            AddResult(group, "Уровень детализации", GetValueOrNotControlled(_template1, BuiltInParameter.VIEW_DETAIL_LEVEL), GetValueOrNotControlled(_template2, BuiltInParameter.VIEW_DETAIL_LEVEL));
            AddResult(group, "Стиль графики", GetValueOrNotControlled(_template1, BuiltInParameter.MODEL_GRAPHICS_STYLE), GetValueOrNotControlled(_template2, BuiltInParameter.MODEL_GRAPHICS_STYLE));
            AddResult(group, "Дисциплина", GetValueOrNotControlled(_template1, BuiltInParameter.VIEW_DISCIPLINE), GetValueOrNotControlled(_template2, BuiltInParameter.VIEW_DISCIPLINE));
        }

        private void CompareCategoryOverrides(string groupName, BuiltInParameter controlParam, CategoryType catType)
        {
            if (!_template1.AreGraphicsOverridesAllowed() || !_template2.AreGraphicsOverridesAllowed()) return;

            bool t1Controls = IsParamControlled(_template1, controlParam);
            bool t2Controls = IsParamControlled(_template2, controlParam);

            if (!t1Controls && !t2Controls) { AddResult(groupName, "Все переопределения", "Не управляется", "Не управляется"); return; }
            if (!t1Controls || !t2Controls) { AddResult(groupName, "Все переопределения", t1Controls ? "Управляется" : "Не управляется", t2Controls ? "Управляется" : "Не управляется"); return; }

            var categories = _doc.Settings.Categories.Cast<Category>().Where(c => c.CategoryType == catType && c.AllowsBoundParameters).ToList();
            foreach (var cat in categories.OrderBy(c => c.Name))
            {
                var ov1 = _template1.GetCategoryOverrides(cat.Id);
                var ov2 = _template2.GetCategoryOverrides(cat.Id);
                bool hidden1 = _template1.GetCategoryHidden(cat.Id);
                bool hidden2 = _template2.GetCategoryHidden(cat.Id);

                if (hidden1 != hidden2 || !AreOverridesEqual(ov1, ov2))
                {
                    string catName = $"{cat.Name}";
                    AddResult(groupName, $"{catName}: Видимость", !hidden1, !hidden2);
                    AddResult(groupName, $"{catName}: Полутона", ov1.Halftone, ov2.Halftone);
                    AddResult(groupName, $"{catName}: Линии проекции (Вес)", ov1.ProjectionLineWeight, ov2.ProjectionLineWeight);
                    AddResult(groupName, $"{catName}: Линии проекции (Цвет)", FormatColor(ov1.ProjectionLineColor), FormatColor(ov2.ProjectionLineColor));
                }
            }
        }

        private void CompareFilters()
        {
            const string group = "Фильтры";
            bool t1Controls = IsParamControlled(_template1, BuiltInParameter.VIS_GRAPHICS_FILTERS);
            bool t2Controls = IsParamControlled(_template2, BuiltInParameter.VIS_GRAPHICS_FILTERS);

            if (!t1Controls && !t2Controls) { AddResult(group, "Применение фильтров", "Не управляется", "Не управляется"); return; }
            if (!t1Controls || !t2Controls) { AddResult(group, "Применение фильтров", t1Controls ? "Управляется" : "Не управляется", t2Controls ? "Управляется" : "Не управляется"); return; }

            var filters1 = _template1.GetFilters().Select(id => _doc.GetElement(id) as ParameterFilterElement).Where(f => f != null).ToList();
            var filters2 = _template2.GetFilters().Select(id => _doc.GetElement(id) as ParameterFilterElement).Where(f => f != null).ToList();
            var allFilterNames = filters1.Select(f => f.Name).Union(filters2.Select(f => f.Name)).Distinct().OrderBy(name => name);

            foreach (var filterName in allFilterNames)
            {
                var f1 = filters1.FirstOrDefault(f => f.Name == filterName);
                var f2 = filters2.FirstOrDefault(f => f.Name == filterName);

                if (f1 != null && f2 == null) { AddResult(group, $"Фильтр: '{filterName}'", "Применен", "Отсутствует"); continue; }
                if (f1 == null && f2 != null) { AddResult(group, $"Фильтр: '{filterName}'", "Отсутствует", "Применен"); continue; }

                if (f1 != null && f2 != null)
                {
                    var ov1 = _template1.GetFilterOverrides(f1.Id);
                    var ov2 = _template2.GetFilterOverrides(f2.Id);
                    bool isHidden1 = _template1.GetFilterVisibility(f1.Id);
                    bool isHidden2 = _template2.GetFilterVisibility(f2.Id);

                    if (isHidden1 != isHidden2 || !AreOverridesEqual(ov1, ov2))
                    {
                        string filterHeader = $"Фильтр '{filterName}'";
                        AddResult(group, $"{filterHeader}: Видимость", !isHidden1, !isHidden2);
                        AddResult(group, $"{filterHeader}: Полутона", ov1.Halftone, ov2.Halftone);
                        AddResult(group, $"{filterHeader}: Линии проекции (Цвет)", FormatColor(ov1.ProjectionLineColor), FormatColor(ov2.ProjectionLineColor));
                    }
                }
            }
        }

        private void CompareViewRange()
        {
            const string group = "Секущий диапазон";

            ViewType[] planViewTypes = { ViewType.FloorPlan, ViewType.CeilingPlan, ViewType.AreaPlan };
            if (!planViewTypes.Contains(_template1.ViewType) || !planViewTypes.Contains(_template2.ViewType))
            {
                return;
            }

            bool t1Controls = IsParamControlled(_template1, BuiltInParameter.PLAN_VIEW_RANGE);
            bool t2Controls = IsParamControlled(_template2, BuiltInParameter.PLAN_VIEW_RANGE);

            if (!t1Controls && !t2Controls) { AddResult(group, "Настройки диапазона", "Не управляется", "Не управляется"); return; }
            if (!t1Controls || !t2Controls) { AddResult(group, "Настройки диапазона", t1Controls ? "Управляется" : "Не управляется", t2Controls ? "Управляется" : "Не управляется"); return; }

            var vp1 = _doc.GetElement(_template1.Id) as ViewPlan;
            var vp2 = _doc.GetElement(_template2.Id) as ViewPlan;

            if (vp1 == null || vp2 == null) { return; }

            var vr1 = vp1.GetViewRange();
            var vr2 = vp2.GetViewRange();

            if (vr1 == null || vr2 == null) { return; }

            AddResult(group, "Верх (Отступ)", vr1.GetOffset(PlanViewPlane.TopClipPlane), vr2.GetOffset(PlanViewPlane.TopClipPlane));
            AddResult(group, "Секущая пл. (Отступ)", vr1.GetOffset(PlanViewPlane.CutPlane), vr2.GetOffset(PlanViewPlane.CutPlane));
            AddResult(group, "Низ (Отступ)", vr1.GetOffset(PlanViewPlane.BottomClipPlane), vr2.GetOffset(PlanViewPlane.BottomClipPlane));
            AddResult(group, "Глубина проецирования (Отступ)", vr1.GetOffset(PlanViewPlane.ViewDepthPlane), vr2.GetOffset(PlanViewPlane.ViewDepthPlane));
        }

        private string FormatColor(Color color)
        {
            if (color == null || !color.IsValid) return "Нет";
            return $"R:{color.Red} G:{color.Green} B:{color.Blue}";
        }
    }
}