enum JOpcode{I32Const, F32Const, GetLocal, BinaryOp, UnaryOp, SetLocal, CreateLocal, Call, GotoIf, Label, Goto, StrConst, CharConst}
class Instruction(JOpcode opcode, string value){
    public JOpcode opcode = opcode;
    public string value = value;

    public override string ToString()
    {
        return "(" + opcode.ToString() + " " + value + ")";
    }
}

class Parameter(string type, string name){
    public string type = type;
    public string name = name;
}

class Function(string returnType, string name, Parameter[] parameters, Instruction[] instructions){
    public readonly string returnType = returnType;
    public readonly string name = name;
    public readonly Parameter[] parameters = parameters;
    public readonly Instruction[] instructions = instructions;

    public dynamic Invoke(VM vm, params dynamic[] args){
        var index = 0;
        Stack<dynamic> stack = [];
        Dictionary<string, dynamic> locals = [];
        Dictionary<string, int> labels = [];

        if(parameters.Length != args.Length){
            throw new Exception("Wrong number of args");
        }
        for(var i=0;i<parameters.Length;i++){
            locals.Add(parameters[i].name, args[i]);
        }

        for(var i=0;i<instructions.Length;i++){
            if(instructions[i].opcode == JOpcode.Label){
                labels.Add(instructions[i].value, i);
            }
        }
        while(true){
            if(index>=instructions.Length){
                return stack.Pop();
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
            if(instr.opcode == JOpcode.CharConst){
                stack.Push(instr.value[0]);
            }
            else if(instr.opcode == JOpcode.GetLocal){
                stack.Push(locals[instr.value]);
            }
            else if(instr.opcode == JOpcode.SetLocal){
                locals[instr.value] = stack.Pop();
            }
            else if(instr.opcode == JOpcode.CreateLocal){
                locals.Add(instr.value, stack.Pop());
            }
            else if(instr.opcode == JOpcode.Call){
                if(instr.value == "Print"){
                    Console.WriteLine(stack.Pop());
                }
                else{
                    var function = vm.GetFunction(instr.value) ?? throw new Exception("Unexpected function: "+instr.value);
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
    public Dictionary<string, Function> functions = [];

    public void Add(Function function){
        functions.Add(function.name, function);
    }

    public Function? GetFunction(string name){
        if(functions.TryGetValue(name, out Function? function)){
            return function;
        }
        return null;
    }
}