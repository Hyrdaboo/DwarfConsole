<h1 align="center">DwarfConsole</h1>
<br>

<img src="https://github.com/Hyrdaboo/DwarfConsole/assets/67780454/c5b779b4-0ae9-4d3b-8355-483e6286f926" width=100%>


# Setup
1. Clone this repo
2. Drag and Drop the addons folder into your project
3. Create a new C# script so that godot creates a new visual studio solution for this project (**this step is crucial**)
4. Build your project by hitting the build button or _alt+B_
5. Go to _Project > Project Settings > Plugins_ find **DwarfConsole** there and enable it
6. Open up Input Map and add a new Action called "ToggleConsole" and add a keybinding. But if you wanna take a look at the demos you'll need this confugiration:
 
   ![image](https://github.com/Hyrdaboo/DwarfConsole/assets/67780454/1ee1f9ba-0a6a-4590-8086-18e9e51366a3)

   **Note** I forgot to put **_Jump_** in this screenshot do include that too.

After completing these steps you are ready to use the console. You can change some of the default settings of the console from _Project > Project Settings > General > DwarfConsole_ 

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

For more examples check out the demo inside the Samples folder. Don't forget to add GlobalSettings.cs to autoload
