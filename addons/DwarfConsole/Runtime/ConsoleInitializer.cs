using Godot;

namespace DwarfConsole
{
    public partial class ConsoleInitializer : Node
    {
        private Node console;
        private bool consoleActive = false;

        public override void _Ready()
        {
            console = ResourceLoader.Load<PackedScene>("res://addons/DwarfConsole/Prefabs/dwarf_console.tscn").Instantiate();
            console.Call("Init", GetTree().Root);
        }

        Node currentScene;
        public override void _Process(double delta)
        {
            if (GetTree().CurrentScene != currentScene && GetTree().CurrentScene != null)
            {
                currentScene = GetTree().CurrentScene;

                GetTree().CurrentScene.TreeExiting += () =>
                {
                    consoleActive = false;
                    if (console.GetParent() != null)
                        GetTree().CurrentScene.RemoveChild(console);
                };
            }

            if (Input.IsActionJustPressed("ToggleConsole"))
            {
                consoleActive = !consoleActive;
                if (consoleActive)
                    GetTree().CurrentScene.AddChild(console);
                else
                    GetTree().CurrentScene.RemoveChild(console);
            }
        }
    }
}
