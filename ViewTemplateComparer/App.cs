using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ViewTemplateComparer
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "RP-Tools";
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Exception) { /* Вкладка уже существует */ }

            RibbonPanel ribbonPanel = application.CreateRibbonPanel(tabName, "Сравнение Шаблонов");

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            PushButtonData buttonData = new PushButtonData(
                "CompareTemplatesButton",
                "Сравнить\nШаблоны",
                assemblyPath,
                "ViewTemplateComparer.Command"
            );

            string resourceName = "ViewTemplateComparer.Resources.img.png";

            ImageSource image = GetEmbeddedImage(resourceName);
            if (image != null)
            {
                buttonData.LargeImage = image;
            }

            ribbonPanel.AddItem(buttonData);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
        private ImageSource GetEmbeddedImage(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null; 

                    var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    return decoder.Frames[0];
                }
            }
            catch
            {
                return null;
            }
        }
    }
}