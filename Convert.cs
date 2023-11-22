using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;


public class Xml2Form {
    static private readonly string TAB = "    ";
    static private readonly string BaseIndent = TAB + TAB;

    static private string ParseColorProperty(string color) {
        if (color.Contains(',')) {
            return $"new System.Drawing.Color.FromArgb({color})";
        } else if (typeof(SystemColors).GetProperty(color) != null) {
            return $"System.Drawing.SystemColors.{color}";
        } else {
            return $"System.Drawing.Color.{color}";
        }
    }

    static private string ParseFontProperty(string font) {
        var font_config = (from config in font.Split(',') select config.TrimStart()).ToArray();
        var font_name = font_config[0];
        var font_size = font_config[1].Substring(0, font_config[1].Length - 2);
        var code = $"new System.Drawing.Font(\"{font_name}\", {font_size}F";
        if (font_config.Length > 2) {
            font_config[2] = font_config[2].Substring(6, font_config[2].Length - 6);
            var styles = new string[font_config.Length - 2];
            for (int i = 2; i < font_config.Length; i++) {
                styles[i - 2] = $"System.Drawing.FontStyle.{font_config[i]}";
            }
            code += $", {String.Join(" | ", styles)}";
        }
        code += ")";
        return code;
    }

    private XElement _root;
    private string _initialize_lines = "";
    private string _statement_lines = "";

    private void InitializeComponent(string type, string name, string[] parameters) {
        _initialize_lines += $"{BaseIndent}this.{name} = new {type}({String.Join(", ", parameters)});" + Environment.NewLine;
        _statement_lines += $"{TAB}private {type} {name};" + Environment.NewLine;
    }

    private void InitializeComponent(string type, string name) {
        _initialize_lines += $"{BaseIndent}this.{name} = new {type}();" + Environment.NewLine;
        _statement_lines += $"{TAB}private {type} {name};" + Environment.NewLine;
    }

    private string ParseXml(XElement xml) {
        var root_name = xml.Attribute("name").Value;
        var root_type = xml.Attribute("type").Value;
        return ParseXml(xml, root_name, root_type);
    }

    private string ParseXml(XElement xml, string root_name, string root_type) {
        var head = $"{BaseIndent}//" + Environment.NewLine
            +      $"{BaseIndent}// {root_name}" + Environment.NewLine
            +      $"{BaseIndent}//" + Environment.NewLine;
        var property_code = "";
        var add_child_code = "";
        var add_event_code = "";
        var add_item_code = "";
        var child_code = "";
        string prefix = xml == _root ? $"{BaseIndent}this." : $"{BaseIndent}this.{root_name}.";
        foreach (var child in xml.Elements()) {
            var child_element_name = child.Name.ToString();
            switch (child_element_name) {
                case "Data":
                case "Param":
                    break;
                case "Object": {
                    var child_name = child.Attribute("name").Value;
                    var child_type = child.Attribute("type").Value.Split(',')[0];
                    add_child_code += $"{prefix}Controls.Add(this.{child.Element("Name").Value});" + Environment.NewLine;
                    InitializeComponent(child_type, child_name);
                    child_code += ParseXml(child);
                    break;
                }
                case "Event": {
                    var event_name = child.Attribute("event_name").Value;
                    var callback_name = child.Value;
                    add_event_code += $"{prefix}{event_name} += new System.EventHandler(this.{callback_name});" + Environment.NewLine;
                    break;
                }
                case "Items":
                case "Columns":
                case "DropDownItems": {
                    var full_child_type = child.Attribute("type").Value;
                    var child_type = full_child_type.Split(',')[0];
                    switch (child_type) {
                        case "System.String": {
                            add_item_code +=
        $@"{prefix}{child_element_name}.AddRange(
            new string[] {{
                {String.Join($",{Environment.NewLine}{BaseIndent}{TAB}{TAB}", from e in child.Elements() select $"\"{e.Value}\"")}
            }}
        );" + Environment.NewLine;
                            break;
                        }
                        case "System.Windows.Forms.ListViewItem": {
                            var items = child.Elements().ToArray();
                            var arr = new string[items.Length];
                            for (int i = 0; i < items.Length; i++) {
                                var item = items[i];
                                var item_name = root_name + item.Name.ToString();
                                var parameter_elements = item.Elements("Param").ToArray();
                                var parameters = new string[] {
                                    $"new string[] {{{String.Join(", ", from e in parameter_elements[0].Elements() select $"\"{e.Value}\"")}}}",
                                    "-1",
                                    "WindowText",
                                    "Window",
                                    "宋体, 9pt"
                                };
                                for (int j = 1; j < parameter_elements.Length; j++) {
                                    var param = parameter_elements[j];
                                    if (param.Value == "") {
                                        continue;
                                    }
                                    parameters[j] = param.Value;
                                }
                                parameters[2] = ParseColorProperty(parameters[2]);
                                parameters[3] = ParseColorProperty(parameters[3]);
                                parameters[4] = ParseFontProperty(parameters[4]);
                                InitializeComponent(child_type, item_name, parameters);
                                child_code += ParseXml(item, item_name, full_child_type);
                                arr[i] = $"this.{item_name}";
                            }
                            property_code +=
        $@"{prefix}{child_element_name}.AddRange(
            new {child_type}[] {{
                {String.Join($",{Environment.NewLine}{BaseIndent}{TAB}{TAB}", arr)}
            }}
        );" + Environment.NewLine;
                            break;
                        }
                        default: {
                            var items = child.Elements().ToArray();
                            var arr = new string[items.Length];
                            for (int i = 0; i < items.Length; i++) {
                                var item = items[i].Element("Object");
                                var item_name = item.Attribute("name").Value;
                                var item_type = item.Attribute("type").Value.Split(',')[0];
                                InitializeComponent(item_type, item_name);
                                child_code += ParseXml(item);
                                arr[i] = $"this.{item_name}";
                            }
                            property_code +=
        $@"{prefix}{child_element_name}.AddRange(
            new {child_type}[] {{
                {String.Join($",{Environment.NewLine}{BaseIndent}{TAB}{TAB}", arr)}
            }}
        );" + Environment.NewLine;
                            break;
                        }
                    }
                    break;
                }
                case "Group": {
                    // TODO: to implement
                    break;
                }
                case "FlatAppearance":{
                    foreach (var p in child.Element("Object").Elements()) {
                        var p_name = p.Name.ToString();
                        if (p_name.Contains("Color")) {
                            property_code += $"{prefix}FlatAppearance.{p_name} = {ParseColorProperty(p.Value)};" + Environment.NewLine;
                        } else {
                            property_code += $"{prefix}FlatAppearance.{p_name} = {p.Value};" + Environment.NewLine;
                        }
                    }
                    break;
                }
                case "ContextMenuStrip": {
                    property_code += $"{prefix}ContextMenuStrip = this.{child.Value};" + Environment.NewLine;
                    break;
                }
                default: {
                    var property = Type.GetType(root_type).GetProperty(child_element_name);
                    if (property == null) {
                        Console.WriteLine($"could not find property {child_element_name} in Type {root_type}");
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
                        property_code += ParseFontProperty(child.Value);
                    } else if (property.PropertyType == typeof(Color)) {
                        property_code += ParseColorProperty(child.Value);
                    } else if (property.PropertyType == typeof(Image)) {
                        if (child.Attribute("mode").Value == "binary")
                            property_code +=
        $@"new System.Drawing.Bitmap(
            new System.IO.MemoryStream(
                System.Convert.FromBase64String(
                    ""{child.Value}""
                )
            )
        )";
                    } else if (property.PropertyType == typeof(Cursor)) {
                        property_code += $"System.Windows.Forms.Cursors.{child.Value}";
                    } else if (class_group.Contains(property.PropertyType)) {
                        property_code += $"new {property.PropertyType.ToString()}({child.Value})";
                    } else {
                        property_code += $"{property.PropertyType.ToString()}.{child.Value}";
                    }
                    property_code += ";" + Environment.NewLine;
                    break;
                }
            }
        }
        return child_code + head + property_code + add_item_code + add_child_code + add_event_code;
    }

    public Xml2Form(string xml) {
        _root = XElement.Parse(xml);
        if (_root.Name == "ObjectCollection") {
            _root = _root.Element("Object");
        }
    }

    public string GetFormCode() {
        var root_name = _root.Attribute("name").Value;
        var root_type = "System.Windows.Forms.Form, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        var main_block = ParseXml(_root, root_name, root_type);
        var code =
$@"public partial class {root_name}: System.Windows.Forms.Form {{
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
        this.SuspendLayout();
{main_block}
        this.ResumeLayout(false);
    }}

    #endregion

{_statement_lines}}}";
        return code;
    }
}
