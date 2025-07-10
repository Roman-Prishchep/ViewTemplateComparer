using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;

namespace ViewTemplateComparer
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Запускаем наше окно
            var window = new ComparisonWindow(uidoc);
            window.ShowDialog();

            return Result.Succeeded;
        }
    }
}