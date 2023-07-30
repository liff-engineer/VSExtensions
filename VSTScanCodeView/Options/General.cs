using System.ComponentModel;
using System.Runtime.InteropServices;

namespace VSTScanCodeView
{
    internal partial class OptionsProvider
    {
        // Register the options with this attribute on your package class:
        // [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "VSTScanCodeView", "General", 0, 0, true, SupportsProfiles = true)]
        [ComVisible(true)]
        public class GeneralOptions : BaseOptionPage<General> { }
    }

    public enum IssueFileOpenMode
    {
        OpenViaProject,
        Open
    }

    public class General : BaseOptionModel<General>
    {

        [Category("配置")]
        [DisplayName("TscanCode路径")]
        [Description("指定自定义的TscanCode.exe路径，如无效则采用内置的。")]
        [DefaultValue("")]
        public string ApplicationPath { get; set; } = "";

        [Category("配置")]
        [DisplayName("文件打开方式")]
        [Description("使用OpenViaProject时作为工程中的文件打开,可能会失败；使用Open则会作为杂项文件打开。")]
        [DefaultValue(IssueFileOpenMode.OpenViaProject)]
        [TypeConverter(typeof(EnumConverter))]
        public IssueFileOpenMode OpenMode { get; set; }= IssueFileOpenMode.OpenViaProject;
    }
}
