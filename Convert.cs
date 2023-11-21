using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Xml.Linq;


public class Xml2Form {
    static private string ParseColorProperty(string color) {
        if (color.Contains(',')) {
            return $"new System.Drawing.Color.FromArgb({color})";
        } else if (typeof(SystemColors).GetProperty(color) != null) {
            return $"System.Drawing.SystemColors.{color}";
        } else {
            return $"System.Drawing.Color.{color}";
        }
    }

    private XElement _root;
    private string _initialize_lines = "";
    private string _statement_lines = "";

    private string ParseElement(XElement element) {
        var name = element.Attribute("name").Value;
        var type = element.Attribute("type").Value;
        var head = "        //" + Environment.NewLine
            +     $"        // {name}" + Environment.NewLine
            +      "        //" + Environment.NewLine;
        var property_code = "";
        var add_child_code = "";
        var add_event_code = "";
        var add_item_code = "";
        var child_code = "";
        string prefix;
        if (element == _root) {
            prefix = "        this.";
            type = "System.Windows.Forms.Form, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        } else {
            prefix = $"        this.{name}.";
        }
        var children = new List<XElement>();
        foreach (var child in element.Elements()) {
            var child_element_name = child.Name.ToString();
            Console.WriteLine(child_element_name);
            switch (child_element_name) {
                case "Object":
                    var child_name = child.Attribute("name").Value;
                    var child_type = child.Attribute("type").Value.Split(',')[0];
                    _initialize_lines += $"        this.{child_name} = new {child_type}();" + Environment.NewLine;
                    _statement_lines += $"    private {child_type} {child_name};" + Environment.NewLine;
                    add_child_code += $"{prefix}Controls.Add(this.{child.Element("Name").Value});" + Environment.NewLine;
                    children.Add(child);
                    break;
                case "Event":
                    var event_name = child.Attribute("event_name").Value;
                    var callback_name = child.Value;
                    add_event_code += $"{prefix}{event_name} += new System.EventHandler(this.{callback_name});" + Environment.NewLine;
                    break;
                case "Items":
                case "Columns":
                case "DropDownItems":
                    child_type = child.Attribute("type").Value.Split(',')[0];
                    if (child_type == "System.String") {
                        add_item_code += $"{prefix}{child_element_name}.AddRange(new string[] {{{String.Join(", ", from e in child.Elements() select $"\"{e.Value}\"")}}});" + Environment.NewLine;
                    } else if (child_type == "System.Windows.Forms.ListViewItem") {
                        // TODO: to implement
                    } else {
                        var items = child.Elements().ToArray();
                        var arr = new string[items.Length];
                        for (int i = 0; i < items.Length; i++) {
                            var item = items[i].Element("Object");
                            var item_name = item.Attribute("name").Value;
                            var item_type = item.Attribute("type").Value.Split(',')[0];
                            _initialize_lines += $"        this.{item_name} = new {item_type}();" + Environment.NewLine;
                            _statement_lines += $"    private {item_type} {item_name};" + Environment.NewLine;
                            children.Add(item);
                            arr[i] = $"this.{item_name}";
                        }
                        property_code += $"{prefix}Columns.AddRange(new {child_type}[] {{{String.Join(", ", arr)}}});" + Environment.NewLine;
                    }
                    break;
                case "FlatAppearance":
                    foreach (var p in child.Element("Object").Elements()) {
                        var p_name = p.Name.ToString();
                        if (p_name.Contains("Color")) {
                            property_code += $"{prefix}FlatAppearance.{p_name} = {ParseColorProperty(p.Value)};" + Environment.NewLine;
                        } else {
                            property_code += $"{prefix}FlatAppearance.{p_name} = {p.Value};" + Environment.NewLine;
                        }
                    }
                    break;
                default:
                    var property = Type.GetType(type).GetProperty(child_element_name);
                    if (property == null) {
                        Console.WriteLine($"could not find property {child_element_name} in Type {type}");
                        continue;
                    }
                    var class_group = new Type[] {
                        typeof(Size),
                        typeof(SizeF),
                        typeof(Point),
                        typeof(Padding)
                    };
                    property_code += $"{prefix}{child_element_name} = ";
                    if (property.PropertyType == typeof(int)) {
                        property_code += child.Value;
                    } else if (property.PropertyType == typeof(string)) {
                        property_code += $"\"{child.Value}\"";
                    } else if (property.PropertyType == typeof(bool)) {
                        property_code += child.Value.ToLower();
                    } else if (property.PropertyType == typeof(Font)) {
                        var font_config = (from config in child.Value.Split(',') select config.TrimStart()).ToArray();
                        var font_name = font_config[0];
                        var font_size = font_config[1].Substring(0, font_config[1].Length - 2);
                        property_code += $"new System.Drawing.Font(\"{font_name}\", {font_size}F";
                        if (font_config.Length > 2) {
                            font_config[2] = font_config[2].Substring(6, font_config[2].Length - 6);
                            var styles = new string[font_config.Length - 2];
                            for (int i = 2; i < font_config.Length; i++) {
                                styles[i - 2] = $"System.Drawing.FontStyle.{font_config[i]}";
                            }
                            property_code += $", {String.Join(" | ", styles)}";
                        }
                        property_code += ")";
                    } else if (property.PropertyType == typeof(Color)) {
                        property_code += ParseColorProperty(child.Value);
                    } else if (property.PropertyType == typeof(Image)) {
                        if (child.Attribute("mode").Value == "binary")
                            property_code += $"new System.Drawing.Bitmap(new System.IO.MemoryStream(System.Convert.FromBase64String(\"{child.Value}\")))";
                    } else if (class_group.Contains(property.PropertyType)) {
                        property_code += $"new {property.PropertyType.ToString()}({child.Value})";
                    } else {
                        property_code += $"{property.PropertyType.ToString()}.{child.Value}";
                    }
                    property_code += ";" + Environment.NewLine;
                    break;
            }
        }
        if (children.Count > 0) {
            foreach (var child in children) {
                child_code += ParseElement(child);
            }
        }
        return child_code + head + property_code + add_item_code + add_child_code + add_event_code;
    }

    public Xml2Form(string xml) {
        _root = XElement.Parse(xml);
    }

    public string GetFormCode() {
        var main_block = ParseElement(_root);
        var code =
$@"public partial class {_root.Attribute("name").Value} {{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name=""disposing"">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {{
        if (disposing && (components != null)) {{
            components.Dispose();
        }}
        base.Dispose(disposing);
    }}

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {{
{_initialize_lines}
        self.SuspendLayout();
{main_block}
        self.ResumeLayout(false);
    }}

    #endregion

{_statement_lines}
}}
";
        return code;
    }
}
