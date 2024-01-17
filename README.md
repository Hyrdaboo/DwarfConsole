# Setup
1. Clone this repo
2. Drag and Drop the addons folder into your project
3. Open up Input Map and add a new Action called "ToggleConsole" and add a keybinding (I set mine to backquote)

# Usage
Inside a node's script:
```C#
using DwarfConsole;
using Godot;

// we need this attribute to tell the console this class has commands
[ConsoleParse]
public partial class MyCommandScript : Node
{

	// this attribute marks the method as a command
	[ConsoleCommand("sum", "adds 2 numbers")]
	public void Sum(int num1, int num2)
	{
		Console.Log($"Result: {num1+num2}");
	}
}

```

For more examples check out the demo inside the Samples folder
