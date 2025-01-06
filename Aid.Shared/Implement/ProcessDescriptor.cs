using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Aid.Shared.Implement
{
    public class ProcessDescriptor
    {
        public string Label { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public List<string> Arguments { get; set; }
        public ProcessStartInfo StartInfo { get; set; }
        public bool IsBackground { get; set; } = false;
        public Dictionary<string,Object> Variables { get; set; }    

        public static ProcessDescriptor Load(JObject json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var descriptor = new ProcessDescriptor();
            descriptor.StartInfo = new ProcessStartInfo();
            var j = json as JObject;
            if(j.TryGetValue("Label", out var label))
            {
                descriptor.Label = label.ToString();
            }
            if(j.TryGetValue("Description",out var description))
            {
                descriptor.Description = description.ToString();
            }
            if (j.TryGetValue("Group", out var group))
            {
                descriptor.Group = group.ToString();
            }
            if (j.TryGetValue("Arguments", out var arguments))
            {
                descriptor.Arguments = arguments.ToObject<List<string>>();
            }
            if (j.TryGetValue("Program",out var program))
            {
                descriptor.StartInfo.FileName = program.ToString();
            }
            if(j.TryGetValue("IsBackground", out var isBackground))
            {
                switch(isBackground.Type)
                {
                    case JTokenType.Boolean:
                        descriptor.IsBackground = isBackground.ToObject<bool>();
                        break;
                    default:
                        descriptor.IsBackground = (isBackground.ToString() == "true");
                        break;
                }
            }
            return descriptor;
        }

        public static List<ProcessDescriptor> Load(string filename, string tag)
        {
            var list = new List<ProcessDescriptor>();
            try
            {
                string content = File.ReadAllText(filename);
                JObject json = JObject.Parse(content);
                if(json.TryGetValue(tag, out var value))
                {
                    List<JObject> objects = value.ToObject<List<JObject>>();
                    foreach (JObject obj in objects)
                    {
                        var descriptor = ProcessDescriptor.Load(obj);
                        list.Add(descriptor);
                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }
            return list;
        }
    }
}
