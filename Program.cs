


enum JOpcode{I32Const, F32Const, GetLocal, BinaryOp, UnaryOp, SetLocal, CreateLocal, Call, GotoIf, Label, Goto}
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

class Tokenizer(string code)
{
    string code = code;
    int index = 0;
    bool split = true;
    int start = 0;
    int depth = 0;
    public List<string> tokens = [];

    void AddPrevToken(int delta = 0){
        if(depth==0){
            if(!split){
                tokens.Add(code[start..(index+delta)]);
            }
            split = true;
        }
    }

    public void Tokenize(){
        var punctuation = ";+-*/=<>";
        var open = "([{";
        var close = ")]}";

        for(index=0;index<code.Length;index++){
            var c = code[index];
            if(close.Contains(c)){
                depth--;
                if(depth<0){
                    throw new Exception("Depth is < 0");
                }
                AddPrevToken(1);
            }
            else if(char.IsWhiteSpace(c)){
                AddPrevToken();
            }
            else if(punctuation.Contains(c)){
                AddPrevToken();
                if(depth==0){
                    tokens.Add(code[index..(index+1)]);
                }
            }
            else if(open.Contains(c)){
                AddPrevToken();
                if(depth==0){
                    split = false;
                    start = index;
                }
                depth++;
            }
            else{
                if(split){
                    split = false;
                    start = index;
                }
            }
        }
        AddPrevToken();
    }

    public static List<string> Tokenize(string code){
        var tokenizer = new Tokenizer(code);
        tokenizer.Tokenize();
        return tokenizer.tokens;
    }
}

static class Parser{
    static List<List<string>> SplitByComma(List<string> tokens){
        List<List<string>> output = [];
        var start = 0;
        for(var i=0;i<tokens.Count;i++){
            if(tokens[i] == ","){
                output.Add(tokens[start..i]);
                start = i+1;
            }
        }
        if(start<tokens.Count){
            output.Add(tokens[start..tokens.Count]);
        }
        return output;
    }

    static Instruction[] ParseExpressionInParens(string code){
        return ParseExpression(Tokenizer.Tokenize(code[1..^1]));
    }

    public static Instruction[] ParseExpression(List<string> tokens){
        var operators = new string[][]{["<", ">"], ["+", "-"], ["/", "*"]};
        if(tokens.Count == 0){
            throw new Exception("No tokens");
        }
        else if(tokens.Count == 1){
            if(tokens[0][0] == '('){
                return ParseExpressionInParens(tokens[0]);
            }
            else if(char.IsDigit(tokens[0][0])){
                if(tokens[0].Contains('.')){
                    return [new Instruction(JOpcode.F32Const, tokens[0])];
                }
                return [new Instruction(JOpcode.I32Const, tokens[0])];
            }
            else if(char.IsLetter(tokens[0][0])){
                return [new Instruction(JOpcode.GetLocal, tokens[0])];
            }
            else{
                throw new Exception("Unexpected token: "+tokens[0]);
            }
        }
        else if(tokens.Count == 2){
            if(char.IsLetter(tokens[0][0]) && tokens[1][0] == '('){
                var args = SplitByComma(Tokenizer.Tokenize(tokens[1][1..^1])).Select(ParseExpression).ToArray();
                return [.. args.SelectMany(a=>a).ToArray(), new Instruction(JOpcode.Call, tokens[0])];
            }
        }
        foreach(var ops in operators){
            var index = tokens.FindLastIndex(t=>ops.Contains(t));
            if(index>=0){
                var left = ParseExpression(tokens[0..index]);
                var right = ParseExpression(tokens[(index+1)..tokens.Count]);
                return [..left, ..right, new Instruction(JOpcode.BinaryOp, tokens[index])];
            }
        }
        throw new Exception("Unexpected tokens");
    }

    static Instruction[] ParseBody(string code, List<string> blocks, ref int labelID){
        var tokens = Tokenizer.Tokenize(code[1..^1]);
        List<Instruction> instructions = [];
        var i = 0;
        while(true){
            if(i>=tokens.Count){
                return [..instructions];
            }
            if(i+1<tokens.Count && tokens[i+1] == "="){
                var end = tokens.IndexOf(";", i+2);
                if(end<0){
                    throw new Exception("No end of expression");
                }
                instructions.AddRange(ParseExpression(tokens[(i+2)..end]));
                instructions.Add(new Instruction(JOpcode.SetLocal, tokens[i]));
                i = end+1;
            }
            else if(tokens[i] == "return"){
                var end = tokens.IndexOf(";", i+1);
                if(end<0){
                    throw new Exception("No end of expression");
                }
                instructions.AddRange(ParseExpression(tokens[(i+1)..end]));
                i = end+1;
            }
            else if(tokens[i] == "if"){
                instructions.AddRange(ParseExpressionInParens(tokens[i+1]));
                instructions.Add(new Instruction(JOpcode.UnaryOp, "!"));
                instructions.Add(new Instruction(JOpcode.GotoIf, labelID.ToString()));
                instructions.AddRange(ParseBody(tokens[i+2], blocks, ref labelID));
                instructions.Add(new Instruction(JOpcode.Label, labelID.ToString()));
                labelID++;
                i+=3;
            }
            else if(tokens[i] == "break"){
                instructions.Add(new Instruction(JOpcode.Goto, blocks[^1]));
                i+=2;
            }
            else if(tokens[i] == "while"){
                var startLabelID = labelID.ToString();
                var endLabelID = (labelID+1).ToString();
                labelID+=2;
                instructions.Add(new Instruction(JOpcode.Label, startLabelID));
                instructions.AddRange(ParseExpressionInParens(tokens[i+1]));
                instructions.Add(new Instruction(JOpcode.UnaryOp, "!"));
                instructions.Add(new Instruction(JOpcode.GotoIf, endLabelID));
                blocks.Add(endLabelID);
                instructions.AddRange(ParseBody(tokens[i+2], blocks, ref labelID));
                blocks.RemoveAt(blocks.Count-1);
                instructions.Add(new Instruction(JOpcode.Goto, startLabelID));
                instructions.Add(new Instruction(JOpcode.Label, endLabelID));
                i+=3;
            }
            else if(tokens[i] == "var"){
                var name = tokens[i+1];
                var end = tokens.IndexOf(";", i+3);
                if(end<0){
                    throw new Exception("No end of expression");
                }
                var expr = ParseExpression(tokens[(i+3)..end]);
                instructions.AddRange(expr);
                instructions.Add(new Instruction(JOpcode.CreateLocal, name));
                i = end+1;
            }
            else{
                var end = tokens.IndexOf(";", i);
                if(end<0){
                    throw new Exception("No end of expression");
                }
                instructions.AddRange(ParseExpression(tokens[i..end]));
                i = end+1;
            }
        }
    }

    static Instruction[] ParseBody(string code){
        List<string> blocks = [];
        int labelID = 0;
        return ParseBody(code, blocks, ref labelID);
    }

    static Parameter[] ParseParameters(string code){
        var parameterTokens = SplitByComma(Tokenizer.Tokenize(code[1..^1]));
        return parameterTokens.Select(p=>new Parameter(p[0], p[1])).ToArray();
    }

    public static Function[] ParseFunctions(string code){
        var tokens = Tokenizer.Tokenize(code);
        List<Function> functions = [];
        var i = 0;
        while(true){
            if(i>=tokens.Count){
                return [..functions];
            }
            var returnType = tokens[i];
            var name = tokens[i+1];
            var parameters = ParseParameters(tokens[i+2]);
            var body = ParseBody(tokens[i+3]);
            functions.Add(new Function(returnType, name, parameters, body));
            i+=4;
        }
    }
}

class Program{

    static void Main(){
        var code = @"
        int Test(int i){
            while(i<100){
                Print(i);
                if(i>10){
                    break;
                }
                i=i+2;
            }
            return i;
        }

        void Main(){
            var i = 0;
            while(i<5){
                Print(i);
                i=i+1;
            }
            var j = Test(i);
            return j;
        }";
        var functions = Parser.ParseFunctions(code);
        var vm = new VM();
        foreach(var f in functions){
            vm.Add(f);
        }
        Console.WriteLine(vm.GetFunction("Main")!.Invoke(vm));
    }
}