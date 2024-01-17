using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DwarfConsole
{
    /// <summary>
    /// Represents a single command for console. 
    /// See <see cref="CommandExecutor.RegisterCommand(string, Command)"/> if
    /// you want to learn how to use this.
    /// </summary>
    public struct Command
    {
        public object Instance;
        public MethodInfo Method;
        public string Description;

        public Command(object instance, MethodInfo method, string description = "No Description.")
        {
            Instance = instance;
            Method = method;
            Description = description;
        }
    }

    /// <summary>
    /// Manages the execution and storing of commands
    /// </summary>
    public class CommandExecutor
    {
        private Dictionary<string, List<Command>> commands = new Dictionary<string, List<Command>>();
        private List<Node> commandNodes = new List<Node>();

        public CommandExecutor ()
        {
            RegisterCommand("help", new Command(this, typeof(CommandExecutor).GetMethod(nameof(ListCommands)), "Displays a list of available commands."));
            RegisterCommand("echo", new Command(this, typeof(CommandExecutor).GetMethod(nameof(Echo)), "Displays messages in console."));
        }

        /// <summary>
        /// Registers a new console command. If a command with the same name
        /// already exists this command will be executed together with the existing command.
        /// <para>
        /// This method is useful for registering C# based classes. Usage example:
        /// <code>
        /// YourClass obj = new YourClass();
        /// RegisterCommand("commandName", new Command(obj, typeof(YourClass).GetMethod(nameof(YourMethod)), "Command description (optional)"));
        /// </code>
        /// Note that if the class instance is disposed the command will no longer work.
        /// </para>
        /// </summary>
        /// <param name="commandName">The name of the command. Entering this message in console will execute the command</param>
        /// <param name="command">The command that will be executed</param>
        public void RegisterCommand(string commandName, Command command)
        {
            if (commands.TryGetValue(commandName, out var result))
            {
                if (!result.Contains(command))
                    result.Add(command);
            }
            else
            {
                commands.Add(commandName, new List<Command> {command});
            }
        }

        /// <summary>
        /// Removes all command entries registered with the specified command name.
        /// </summary>
        /// <param name="commandName">The name of the command</param>
        public void UnregisterCommand(string commandName)
        {
            commands.Remove(commandName);
        }

        /// <summary>
        /// Executes a command with the specified name. Called internally
        /// by the <see cref="Console"/>. Calling this directly isn't advised.
        /// </summary>
        /// <param name="rawCommand"></param>
        public void ExecuteCommand(string rawCommand)
        {
            if (string.IsNullOrEmpty(rawCommand))
                return;

            string[] cmdComponents = ProcessCommand(rawCommand);

            if (commands.TryGetValue(cmdComponents[0], out var result))
            {
                foreach (var command in result)
                {
                    try
                    {
                        ParameterInfo[] parameterInfos = command.Method.GetParameters();
                        if (cmdComponents.Length - 1 != parameterInfos.Length)
                        {
                            Console.Log("Invalid arguments count.", Console.LogType.Error);
                            return;
                        }

                        object[] parameters = new object[parameterInfos.Length];

                        for (int i = 0; i < parameterInfos.Length; i++)
                        {
                            Type parameterType = parameterInfos[i].ParameterType;

                            parameters[i] = Convert.ChangeType(cmdComponents[i+1], parameterType);
                        }

                        // I know this is stupid but couldn't find another way
                        // of handling invalid(deleted) objects. Hopefully
                        // this doesn't haunt me back later.
                        try
                        {
                            command.Instance.ToString();
                        }
                        catch (ObjectDisposedException)
                        {
                            UnregisterCommand(cmdComponents[0]);
                            Console.Log("The object that defines this command has been disposed. The command will be removed.", Console.LogType.Error);
                            return;
                        }

                        command.Method.Invoke(command.Instance, parameters);
                    }
                    catch (Exception e)
                    {
                        Console.Log($"An unexpected error occured: {e.Message}", Console.LogType.Error);
                        GD.PrintErr(e);
                    }
                }
            }
            else
                Console.Log($"Unrecognized command '{cmdComponents[0]}'.", Console.LogType.Error);
        }

        private string[] ProcessCommand(string rawCommand)
        {
            rawCommand = rawCommand.Trim();
            List<string> parts = new List<string>();

            Regex regex = new Regex(@"(""[^""]+"")|\S+");

            MatchCollection matches = regex.Matches(rawCommand);

            foreach (Match match in matches)
            {
                parts.Add(match.Value.Trim('"'));
            }

            return parts.ToArray();
        }

        /// <summary>
        /// Registers a node's methods for execution. In order for this to work
        /// the node:
        /// <list type="number">
        /// <item><description>Must be inside the tree</description></item>
        /// <item><description>Must be valid ie. <see cref="GodotObject.IsInstanceValid(GodotObject)"/> should be true</description></item>
        /// <item><description>The node must have a script with <c>[ConsoleParse]</c> attribute</description></item> 
        /// </list>
        /// If the above conditions are satisfied and the node has not been registered already this method will loop through
        /// the node's script's methods and register the ones marked with <c>[ConsoleCommand]</c> attribute as commands.
        /// <para>
        /// <strong>Note:</strong> Calling this manually isn't advised. Node registration is already managed
        /// by the <seealso cref="Console"/>
        /// </para>
        /// </summary>
        public void TryRegisterNode(Node node)
        {
            if (!GodotObject.IsInstanceValid(node) || !node.IsInsideTree())
                return;

            Type nodeType = node.GetType();
            if (nodeType.GetCustomAttribute<ConsoleParseAttribute>() != null)
            {
                if (commandNodes.Contains(node))
                    return;
                commandNodes.Add(node);

                MethodInfo[] consoleCommandMethods = nodeType.GetMethods();

                foreach (MethodInfo method in consoleCommandMethods)
                {
                    ConsoleCommandAttribute attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();

                    if (attribute != null)
                    {
                        RegisterCommand(attribute.name, new Command(node, method, attribute.description));
                    }
                }
            }
        }

        /// <summary>
        /// Unregisters an already registered node. Calling this directly
        /// is not recommended. Unregistering nodes is already managed
        /// by the <seealso cref="Console"/>
        /// </summary>
        /// <param name="node"></param>
        public void TryUnregisterNode(Node node)
        {
            if (commandNodes.Remove(node))
            {
                MethodInfo[] consoleCommandMethods = node.GetType().GetMethods();

                foreach (MethodInfo method in consoleCommandMethods)
                {
                    ConsoleCommandAttribute attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();

                    if (attribute != null)
                    {
                        UnregisterCommand(attribute.name);
                    }
                }
            }
        }

        /// <summary>
        /// Lists all the available commands in Console.
        /// Use the <i>help</i> command in Console to execute this function.
        /// </summary>
        public void ListCommands()
        {
            foreach (var command in commands)
            {
                foreach (var value in command.Value)
                {
                    string paramTypes = "";
                    foreach (var p in value.Method.GetParameters())
                    {
                        paramTypes += $"{p.ParameterType}, ";
                    }
                    if (!string.IsNullOrEmpty(paramTypes))
                        paramTypes = paramTypes.Substring(0, paramTypes.Length - 2);

                    Console.Log($"{command.Key} - {value.Description}  |  Caller:[{value.Instance.GetType()}.{value.Method.Name}({paramTypes})]");
                }
            }
        }

        /// <summary>
        /// Prints a message to the Console.
        /// Use the <i>echo</i> command in Console to execute this function.
        /// Spaced out text should be between quotes eg. <i>echo "Hello World"</i>
        /// </summary>
        /// <param name="message">The message to be printed</param>
        public void Echo(string message)
        {
            Console.Log(message);
        }
    }
}
