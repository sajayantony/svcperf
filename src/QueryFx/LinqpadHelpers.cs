namespace EtlViewer.QueryFx
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;

    class LinqpadHelpers
    {
        public static string ExtractQuery(string fileName, TextWriter errorlogger)
        {
            try
            {
                string data = File.ReadAllText(fileName);
                XmlParserContext context = new XmlParserContext(null, null, null, XmlSpace.None);
                Stream xmlFragment = new MemoryStream(Encoding.UTF8.GetBytes(data));
                XmlTextReader reader = new XmlTextReader(xmlFragment, XmlNodeType.Element, context);
                reader.MoveToContent();
                if (reader.NodeType == XmlNodeType.Text)
                {
                    return data;
                }
                XmlReader reader2 = reader.ReadSubtree();
                StringBuilder output = new StringBuilder();
                using (XmlWriter writer = XmlWriter.Create(output))
                {
                    writer.WriteNode(reader2, true);
                }

                StringReader reader3 = new StringReader(data);
                for (int i = 0; i < reader.LineNumber; i++)
                {
                    reader3.ReadLine();
                }
                return reader3.ReadToEnd().Trim();           
            }
            catch (Exception ex)
            {                
                errorlogger.WriteLine(ex.Message);
                errorlogger.WriteLine(ex.StackTrace);
            }

            return string.Format("//Error loading the file - {0} .The the linq file might be a linqpad expression which is not supported in the viewer currently." ,fileName);
        }
    }
}
