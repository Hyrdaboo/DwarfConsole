using DwarfConsole;
using Godot;

[ConsoleParse]
public partial class GlobalSettings : Node
{

    [ConsoleCommand("set_br", "sets the screen brightness")]
    public void SetBrightness(float brightness)
    {
        brightness = Mathf.Max(brightness, 0);

        WorldEnvironment we = GetTree().CurrentScene.FindChildOfType<WorldEnvironment>() as WorldEnvironment;

        if (we != null)
        {
            we.Environment.AdjustmentEnabled = true;
            we.Environment.AdjustmentBrightness = brightness;
            GD.Print(we.Environment.AdjustmentBrightness);
        }
    }

    [ConsoleCommand("set_res", "sets the screen resolution")]
    public void SetResolution(string resolution)
    {
        Vector2I res = -Vector2I.One;

        try
        {
            string[] components = resolution.Split('x');
            res.X = int.Parse(components[0]);
            res.Y = int.Parse(components[1]);
        }
        catch (System.Exception)
        {
            Console.Log("Wrong formatting. Try something like 1280x720", Console.LogType.Error);
            return;
        }

        GetWindow().Size = res;
    }
}
