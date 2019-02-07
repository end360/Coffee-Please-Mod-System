using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using DevConsole.Internal;
using DevConsole;

namespace Modding
{
    class CommandTypeInfo
    {
        public ConstructorInfo constructor
        {
            get;
            private set;
        }

        public ParameterInfo firstParameter
        {
            get;
            private set;
        }

        public bool isFunc
        {
            get;
            private set;
        }

        public bool isGeneric
        {
            get;
            private set;
        }

        public int parametersLength
        {
            get;
            private set;
        }

        public Type type
        {
            get;
            private set;
        }

        public CommandTypeInfo(Type type)
        {
            this.type = type;
            this.isGeneric = (!type.IsGenericType ? false : type.IsGenericTypeDefinition);
            this.SetConstructor();
            this.SetExtraInfo();
        }

        public CommandTypeInfo MakeGeneric(Type[] paramTypes)
        {
            return new CommandTypeInfo(this.type.MakeGenericType(paramTypes));
        }

        private void SetConstructor()
        {
            bool flag = false;
            ConstructorInfo[] constructors = this.type.GetConstructors();
            for (int i = 0; i < (int)constructors.Length; i++)
            {
                if (constructors[i].IsPublic)
                {
                    ParameterInfo[] parameters = constructors[i].GetParameters();
                    if (((int)parameters.Length < 3 || !parameters[0].ParameterType.IsSubclassOf(typeof(Delegate)) || parameters[1].ParameterType != typeof(string) ? false : parameters[2].ParameterType == typeof(string)))
                    {
                        this.constructor = constructors[i];
                        this.firstParameter = parameters[0];
                        flag = true;
                        break;
                    }
                }
            }
            if (!flag)
            {
                throw new InvalidCommandTypeConstructorException(this.type);
            }
        }

        private void SetExtraInfo()
        {
            MethodInfo method = this.firstParameter.ParameterType.GetMethod("Invoke");
            this.isFunc = method.ReturnType != typeof(void);
            this.parametersLength = (int)method.GetParameters().Length;
        }
    }
    class CommandAttributeVerifier
    {
        private MethodInfo method;

        private CommandAttribute attribute;

        private CommandTypeInfo commandType;

        public bool hasCommandAttribute
        {
            get
            {
                return this.attribute != null;
            }
        }

        public CommandAttributeVerifier(MethodInfo method)
        {
            this.method = method;
            this.attribute = Attribute.GetCustomAttribute(method, typeof(CommandAttribute)) as CommandAttribute;
        }

        private static bool BothAreAction(MethodInfo method, CommandTypeInfo commandType)
        {
            return (method.ReturnType != typeof(void) ? false : !commandType.isFunc);
        }

        private static bool BothAreFunc(MethodInfo method, CommandTypeInfo commandType)
        {
            return (method.ReturnType == typeof(void) ? false : commandType.isFunc);
        }

        private void CheckCommandTypeMatch(CommandTypeInfo[] commandTypes)
        {
            ParameterInfo[] parameters = this.method.GetParameters();
            Type[] parameterType = new Type[(int)parameters.Length];
            for (int i = 0; i < (int)parameters.Length; i++)
            {
                parameterType[i] = parameters[i].ParameterType;
            }
            for (int j = 0; j < (int)commandTypes.Length; j++)
            {
                if ((int)parameters.Length == commandTypes[j].parametersLength)
                {
                    if (CommandAttributeVerifier.BothAreAction(this.method, commandTypes[j]))
                    {
                        if (!commandTypes[j].isGeneric)
                        {
                            this.commandType = commandTypes[j];
                        }
                        else
                        {
                            this.commandType = commandTypes[j].MakeGeneric(parameterType);
                        }
                        break;
                    }
                    else if (CommandAttributeVerifier.BothAreFunc(this.method, commandTypes[j]))
                    {
                        if (!commandTypes[j].isGeneric)
                        {
                            this.commandType = commandTypes[j];
                        }
                        else
                        {
                            this.commandType = commandTypes[j].MakeGeneric(parameterType.Concat<Type>((IEnumerable<Type>)(new Type[] { this.method.ReturnType })).ToArray<Type>());
                        }
                        break;
                    }
                }
            }
        }

        public CommandBase ExtractCommand(CommandTypeInfo[] commandTypes)
        {
            CommandBase commandBase = null;
            if (!this.IsDeclarationSupported())
            {
                throw new UnsupportedCommandDeclarationException(this.method);
            }
            this.CheckCommandTypeMatch(commandTypes);
            if (this.commandType == null)
            {
                throw new NoSuitableCommandFoundException(this.method);
            }
            commandBase = (CommandBase)Activator.CreateInstance(this.commandType.type, new object[] { Delegate.CreateDelegate(this.commandType.firstParameter.ParameterType, this.method), this.attribute.@group, this.attribute.@alias, this.attribute.help });
            return commandBase;
        }

        private bool IsDeclarationSupported()
        {
            return (!this.method.IsStatic || this.method.IsGenericMethod ? false : !this.method.IsGenericMethodDefinition);
        }
    }

    class CommandLoader
    {
        private Assembly assembly;
        private Type[] types;
        private DevConsole.CommandBase[] commands;
        private CommandTypeInfo[] commandTypes;

        public CommandLoader(Assembly asm)
        {
            assembly = asm;
            LoadTypesFromAssembly();
            commandTypes = FilterCommandTypes(LoadTypesFromAssembly(Assembly.GetAssembly(typeof(DevConsole.CommandBase))));
        }

        void LoadTypesFromAssembly()
        {
            if (types != null || assembly == null)
                return;

            types = (
                from x in assembly.GetTypes()
                where x.IsClass == true ? true : (!x.IsValueType ? false : !x.IsEnum)
                select x).ToArray<Type>();
        }

        public static Type[] LoadTypesFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                return new Type[0];

            return (
                from x in assembly.GetTypes()
                where x.IsClass == true ? true : (!x.IsValueType ? false : !x.IsEnum)
                select x).ToArray<Type>();
        }

        static CommandTypeInfo[] FilterCommandTypes(Type[] types)
        {
            List<CommandTypeInfo> commandTypeInfos = new List<CommandTypeInfo>();
            foreach(Type t in types)
            {
                try
                {
                    if(t != null && t.IsSubclassOf(typeof(CommandBase)))
                    {
                        commandTypeInfos.Add(new CommandTypeInfo(t));
                    }
                }
                catch (ConsoleException ce)
                {
                    DevConsole.Console.LogWarning(ce);
                }
                catch(Exception e)
                {
                    DevConsole.Console.LogError(e);
                }
            }
            return commandTypeInfos.ToArray();
        }

        DevConsole.CommandBase[] LoadCommansInType(Type t)
        {
            List<CommandBase> temp = new List<DevConsole.CommandBase>();
            MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            foreach(MethodInfo method in methods)
            {
                try
                {
                    CommandAttributeVerifier cav = new CommandAttributeVerifier(method);
                    if (cav.hasCommandAttribute)
                    {
                        CommandBase command = cav.ExtractCommand(commandTypes);
                        if (command != null)
                        {
                            temp.Add(command);
                        }
                    }
                }catch(ConsoleException ce)
                {
                    DevConsole.Console.LogError(ce);
                }
            }
            return temp.ToArray();
        }

        public DevConsole.CommandBase[] GetCommands()
        {
            if(commands == null)
            {
                List<DevConsole.CommandBase> temp = new List<DevConsole.CommandBase>();
                foreach(Type t in types)
                {
                    temp.AddRange(LoadCommansInType(t));
                }
                commands = temp.ToArray();
            }

            return commands;
        }
        
    }
}
