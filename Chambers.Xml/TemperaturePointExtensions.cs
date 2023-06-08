using System.Globalization;
using System.Xml.Linq;
using Chambers.Core;

namespace Chambers.Xml;

public static class TemperaturePointExtensions
{
    public static void Save(this IEnumerable<IReadOnlyTemperaturePoint> collection, string path)
    {
        var doc = new XDocument();
        var elements = new XElement("points");

        foreach (var t in collection)
        {
            string time = t.Time.ToString(CultureInfo.InvariantCulture);
            string monitored = string.Format(CultureInfo.InvariantCulture, "{0:0.000}", t.Monitored);
            string target = string.Format(CultureInfo.InvariantCulture, "{0:0.000}", t.Target);

            elements.Add(
                new XElement("point",
                    new XAttribute("time", time),
                    new XAttribute("monitored", monitored),
                    new XAttribute("target", target)));
        }
        doc.Add(elements);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        doc.Save(path);
    }
}
