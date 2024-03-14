using Mono.Cecil;
using Mono.Cecil.Cil;

class EmitIL{
    public static void Emit(){
        var mscorlib = ModuleDefinition.ReadModule("C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\mscorlib.dll");

        // Create a new type
        var myHelloWorldApp = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("HelloWorld", new Version(1, 0, 0, 0)), "HelloWorld", ModuleKind.Console);

        var module = myHelloWorldApp.MainModule;

        // create the program type and add it to the module
        var programType = new TypeDefinition("HelloWorld", "Program",
            Mono.Cecil.TypeAttributes.Class | Mono.Cecil.TypeAttributes.Public, module.TypeSystem.Object);

        module.Types.Add(programType);

        // add an empty constructor
        var ctor = new MethodDefinition(".ctor", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig
            | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName, module.TypeSystem.Void);

        // create the constructor's method body
        var il = ctor.Body.GetILProcessor();

        il.Append(il.Create(OpCodes.Ldarg_0));

        var objectCtor = mscorlib.GetType("System.Object").Methods.Where(m => m.IsConstructor && !m.HasParameters).First();
        // call the base constructor
        il.Append(il.Create(OpCodes.Call, module.ImportReference(objectCtor)));

        il.Append(il.Create(OpCodes.Nop));
        il.Append(il.Create(OpCodes.Ret));

        programType.Methods.Add(ctor);

        // define the 'Main' method and add it to 'Program'
        var mainMethod = new MethodDefinition("Main",
            Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Void);

        programType.Methods.Add(mainMethod);

        // add the 'args' parameter
        var argsParameter = new ParameterDefinition("args",
            Mono.Cecil.ParameterAttributes.None, new ArrayType(module.TypeSystem.String));

        mainMethod.Parameters.Add(argsParameter);

        // create the method body
        il = mainMethod.Body.GetILProcessor();

        il.Append(il.Create(OpCodes.Nop));
        il.Append(il.Create(OpCodes.Ldstr, "Hello World"));
        
        var netfxConsole = mscorlib.GetType("System.Console");
        // Obtain the correct WriteLine method, there might be a better way
        var writeLine = netfxConsole
                        .Methods
                        .Where(m => m.Name == "WriteLine" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType == mscorlib.TypeSystem.String)
                        .First();
        var writeLineMethod = il.Create(OpCodes.Call,module.ImportReference(writeLine));

        // call the method
        il.Append(writeLineMethod);

        il.Append(il.Create(OpCodes.Nop));
        il.Append(il.Create(OpCodes.Ret));

        // set the entry point and save the module
        myHelloWorldApp.EntryPoint = mainMethod;
        myHelloWorldApp.Write("HelloWorld.exe");
    }
}