#if TOOLS
using Godot;
using Godot.Collections;

[Tool]
public partial class Plugin : EditorPlugin
{
	public override void _EnterTree()
	{
		GD.Print("PLUGIN START");
		TrySetSetting("DwarfConsole/Defaults/NormalColor", new Color("white"), Variant.Type.Color, "Text color used for normal messages");
		TrySetSetting("DwarfConsole/Defaults/WarningColor", new Color("#ebcb2f"), Variant.Type.Color, "Text color used for warning messages");
		TrySetSetting("DwarfConsole/Defaults/ErrorColor", new Color("#eb4b2f"), Variant.Type.Color, "Text color used for error messages");

		AddAutoloadSingleton("ConsoleInitializer", "res://addons/DwarfConsole/Runtime/ConsoleInitializer.cs");
    }

	public override void _ExitTree()
	{

	}

	private void TrySetSetting(string name, Variant defaultValue, Variant.Type type, string hintString = "", PropertyHint hint = PropertyHint.None)
	{
		var settingInfo = new Dictionary
		{
			{ "name", name },
			{ "type", (int)type },
			{ "hint", (int)hint },
			{ "hint_string", hintString }
		};

		var value = ProjectSettings.HasSetting(name) ? ProjectSettings.GetSetting(name) : defaultValue;
        ProjectSettings.SetSetting(name, value);
		ProjectSettings.AddPropertyInfo(settingInfo);
		ProjectSettings.SetInitialValue(name, defaultValue);
        ProjectSettings.Save();
    }
}
#endif
