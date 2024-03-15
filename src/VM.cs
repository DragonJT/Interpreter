
enum JOpcode{I32Const, F32Const, GetLocal, BinaryOp, UnaryOp, SetLocal, CreateLocal, 
    Call, GotoIf, Label, Goto, StrConst, CharConst, Delegate}

class Instruction(JOpcode opcode, string value){
    public JOpcode opcode = opcode;
    public string value = value;

    public override string ToString(){
        return "(" + opcode.ToString() + " " + value + ")";
    }
}

class Variable(string type, string name){
    public string type = type;
    public string name = name;
}

class Function(Function? parent, string returnType, string name, Variable[] parameters){
    public readonly Function? parent = parent;
    public readonly string returnType = returnType;
    public readonly string name = name;
    public readonly Variable[] parameters = parameters;
    public List<Instruction> instructions = [];
    List<Variable> locals = [];
    Dictionary<string, int> labels = [];
    Stack<Dictionary<string, dynamic?>> localStack = [];

    public void Compile(){
        for(var i=0;i<instructions.Count;i++){
            if(instructions[i].opcode == JOpcode.Label){
                labels.Add(instructions[i].value, i);
            }
        }
        foreach(var i in instructions){
            if(i.opcode == JOpcode.CreateLocal){
                locals.Add(new Variable("Unknown", i.value));
                i.opcode = JOpcode.SetLocal;
            }
        }
    }

    Dictionary<string, dynamic?> CurrentLocals => localStack.Peek();

    bool TryGetLocal(string name, out dynamic? value){
        if(CurrentLocals.TryGetValue(name, out value)){
            return true;
        }
        else{
            if(parent!=null){
                return parent.TryGetLocal(name, out value);
            }
            else{
               value = null;
               return false;
            }
        }
    }

    dynamic? GetLocal(string name){
        if(TryGetLocal(name, out dynamic? value)){
            return value;
        }
        throw new Exception("Cant find local with name: "+name);
    }

    void SetLocal(string name, dynamic value){
        if(CurrentLocals.ContainsKey(name)){
            CurrentLocals[name] = value;
        }
        else{
            if(parent!=null){
                parent.SetLocal(name, value);
            }
            else{
                throw new Exception("Cant set local with name: "+name);
            }
        }
    }

    public dynamic? Invoke(VM vm, params dynamic[] args){
        var index = 0;
        Stack<dynamic> stack = [];

        if(parameters.Length != args.Length){
            throw new Exception("Wrong number of args");
        }
        localStack.Push([]);
        var currentLocals = CurrentLocals;
        for(var i=0;i<parameters.Length;i++){
            currentLocals.Add(parameters[i].name, args[i]);
        }
        foreach(var l in locals){
            currentLocals.Add(l.name, null);
        }
        
        while(true){
            if(index>=instructions.Count){
                localStack.Pop();
                if(returnType!="void"){
                    return stack.Pop();
                }
                else{
                    return null;
                }
            }
            var instr = instructions[index];
            if(instr.opcode == JOpcode.Label){}
            else if(instr.opcode == JOpcode.GotoIf){
                if(stack.Pop()){
                    index = labels[instr.value];
                }
            }
            else if(instr.opcode == JOpcode.Goto){
                index = labels[instr.value];
            }
            else if(instr.opcode == JOpcode.I32Const){
                stack.Push(int.Parse(instr.value));
            }
            if(instr.opcode == JOpcode.F32Const){
                stack.Push(float.Parse(instr.value));
            }
            else if(instr.opcode == JOpcode.StrConst){
                stack.Push(instr.value);
            }
            else if(instr.opcode == JOpcode.Delegate){
                stack.Push(instr.value);
            }
            else if(instr.opcode == JOpcode.CharConst){
                stack.Push(instr.value[0]);
            }
            else if(instr.opcode == JOpcode.GetLocal){
                stack.Push(GetLocal(instr.value));
            }
            else if(instr.opcode == JOpcode.SetLocal){
                SetLocal(instr.value, stack.Pop());
            }
            else if(instr.opcode == JOpcode.Call){
                if(instr.value == "Print"){
                    Console.WriteLine(stack.Pop());
                }
                else{
                    Function? function;
                    if(TryGetLocal(instr.value, out dynamic? local)){
                        function = vm.GetFunction(local);
                        if(function == null){
                            throw new Exception("Cant find function: "+local);
                        }
                    }
                    else{
                        function = vm.GetFunction(instr.value);
                        if(function == null){
                            throw new Exception("Cant find function: "+instr.value);
                        }
                    }
                    var plength = function.parameters.Length;
                    var fargs = new dynamic[plength];
                    for(var i=plength-1;i>=0;i--){
                        fargs[i] = stack.Pop();
                    }
                    var returnValue = function.Invoke(vm, fargs);
                    if(function.returnType!="void"){
                        stack.Push(returnValue);
                    }
                }
            }
            else if(instr.opcode == JOpcode.UnaryOp){
                var value = stack.Pop();
                var op = instr.value;
                if(op == "!"){
                    stack.Push(!value);
                }
                else{
                    throw new Exception("Unexpected UnaryOp: "+op);
                }
            }
            else if(instr.opcode == JOpcode.BinaryOp){
                var right = stack.Pop();
                var left = stack.Pop();
                var op = instr.value;
                if(op=="+"){
                    stack.Push(left+right);
                }
                else if(op=="*"){
                    stack.Push(left*right);
                }
                else if(op=="-"){
                    stack.Push(left-right);
                }
                else if(op=="/"){
                    stack.Push(left/right);
                }
                else if(op=="<"){
                    stack.Push(left<right);
                }
                else if(op==">"){
                    stack.Push(left>right);
                }
                else{
                    throw new Exception("Unexpected BinaryOp: "+op);
                }
            }
            index++;
        }
    }
}

class VM{
    public List<Function> functions = [];

    public void Add(Function function){
        functions.Add(function);
        function.Compile();
    }

    public Function? GetFunction(string name){
        return functions.FirstOrDefault(f=> f.name== name);
    }
}