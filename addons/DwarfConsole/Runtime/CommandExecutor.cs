using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DwarfConsole
{
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

    public class CommandExecutor
    {
        private Dictionary<string, List<Command>> commands = new Dictionary<string, List<Command>>();
        private List<Node> commandNodes = new List<Node>();

        public CommandExecutor ()
        {
            RegisterCommand("help", new Command(this, typeof(CommandExecutor).GetMethod(nameof(ListCommands)), "Displays a list of available commands."));
            RegisterCommand("echo", new Command(this, typeof(CommandExecutor).GetMethod(nameof(Echo)), "Displays messages in console."));
        }

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

        public void UnregisterCommand(string commandName)
        {
            commands.Remove(commandName);
        }

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

        public void ListCommands()
        {
            foreach (var command in commands)
            {
                foreach (var value in command.Value)
                {
                    Console.Log($"{command.Key} - {value.Description}  |  Caller:[{value.Instance.GetType()}.{value.Method.Name}]");
                }
            }
        }

        public void Echo(string message)
        {
            Console.Log(message);
        }
    }
}
