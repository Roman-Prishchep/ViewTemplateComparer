using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ViewTemplateComparer
{
    public class HtmlGenerator
    {
        private readonly View _template1;
        private readonly View _template2;

        public HtmlGenerator(View template1, View template2)
        {
            _template1 = template1;
            _template2 = template2;
        }

        public string Generate(List<ComparisonResult> results)
        {
            var sb = new StringBuilder();

            // Разделяем результаты на отличия и совпадения
            var differences = results.Where(r => r.IsDifferent).ToList();
            var matches = results.Where(r => !r.IsDifferent).ToList();

            // --- HTML Boilerplate и стили ---
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='ru'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='UTF-8'>");
            sb.AppendLine("<title>Отчет о сравнении шаблонов</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine("h1, h2 { color: #2E4053; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 30px; }");
            sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            sb.AppendLine("th { background-color: #f2f2f2; }");
            sb.AppendLine("tr.difference { background-color: #ffecec; }");
            sb.AppendLine("tr.group-header td { background-color: #AED6F1; font-weight: bold; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine($"<h1>Отчет о сравнении шаблонов</h1>");
            sb.AppendLine($"<p><b>Шаблон 1:</b> {_template1.Name}</p>");
            sb.AppendLine($"<p><b>Шаблон 2:</b> {_template2.Name}</p>");

            // --- Секция "Отличия" ---
            sb.AppendLine("<h2>Отличия</h2>");
            if (differences.Any())
            {
                sb.AppendLine("<table>");
                sb.AppendLine($"<tr><th>Параметр</th><th>{_template1.Name}</th><th>{_template2.Name}</th></tr>");
                AppendResultsToTable(sb, differences, true);
                sb.AppendLine("</table>");
            }
            else
            {
                sb.AppendLine("<p>Отличий не найдено.</p>");
            }

            // --- Секция "Совпадения" ---
            sb.AppendLine("<h2>Совпадения</h2>");
            if (matches.Any())
            {
                sb.AppendLine("<table>");
                sb.AppendLine($"<tr><th>Параметр</th><th>{_template1.Name}</th><th>{_template2.Name}</th></tr>");
                AppendResultsToTable(sb, matches, false);
                sb.AppendLine("</table>");
            }
            else
            {
                sb.AppendLine("<p>Совпадений не найдено.</p>");
            }


            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private void AppendResultsToTable(StringBuilder sb, List<ComparisonResult> results, bool isDifference)
        {
            string currentGroup = null;
            foreach (var group in results.GroupBy(r => r.GroupName))
            {
                // Заголовок группы
                sb.AppendLine($"<tr class='group-header'><td colspan='3'>{group.Key}</td></tr>");

                foreach (var result in group)
                {
                    string rowClass = isDifference ? "class='difference'" : "";
                    sb.AppendLine($"<tr {rowClass}>");
                    sb.AppendLine($"<td>{result.ParameterName}</td>");
                    sb.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(result.Value1)}</td>");
                    sb.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(result.Value2)}</td>");
                    sb.AppendLine("</tr>");
                }
            }
        }
    }
}