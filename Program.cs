

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

enum TokenType{Varname, Int, Float, Curly, Square, Parens, Punctuation}

class Token(string value, int start, int end, TokenType type){
    public string value = value;
    public int start = start;
    public int end = end;
    public TokenType type = type;
}

static class Tokenizer{
    public static List<Token> Tokenize(string code){
        var index = 0;
        var tokens = new List<Token>();
        var open = "({[";
        var close = ")}]";

        loop:
        if(index>=code.Length){
            return tokens;
        }
        var c = code[index];
        if(char.IsWhiteSpace(c)){
            index++;
            goto loop;
        }
        if(char.IsLetter(c)){
            var start = index;
            index++;
            while(true){
                if(index>=code.Length){
                    tokens.Add(new Token(code[start..index], start, index, TokenType.Varname));
                    return tokens;
                }
                c = code[index];
                if(char.IsLetter(c) || char.IsDigit(c)){
                    index++;
                    continue;
                }
                tokens.Add(new Token(code[start..index], start, index, TokenType.Varname));
                goto loop;
            }
        }
        else if(char.IsDigit(c)){
            var start = index;
            index++;
            var type = TokenType.Int;
            while(true){
                if(index>=code.Length){
                    tokens.Add(new Token(code[start..index], start, index, type));
                    return tokens;
                }
                c = code[index];
                if(c=='.'){
                    type = TokenType.Float;
                    index++;
                    continue;
                }
                else if(char.IsDigit(c)){
                    index++;
                    continue;
                }
                tokens.Add(new Token(code[start..index], start, index, type));
                goto loop;
            }
        }
        else if(open.Contains(c)){
            var start = index;
            var depth = 1;
            index++;
            while(true){
                if(index>=code.Length){
                    throw new Exception("No close braces before end of file");
                }
                c = code[index];
                if(open.Contains(c)){
                    index++;
                    depth++;
                    continue;
                }
                else if(close.Contains(c)){
                    index++;
                    depth--;
                    if(depth<1){
                        if(code[start] == '{' && code[index-1]=='}'){
                            tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.Curly));
                            goto loop;
                        }
                        else if(code[start] == '[' && code[index-1]==']'){
                            tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.Square));
                            goto loop;
                        }
                        else if(code[start] == '(' && code[index-1]==')'){
                            tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.Parens));
                            goto loop;
                        }
                        else{
                            throw new Exception("Unexpected start and end of braces: "+code[start]+code[index-1]);
                        }
                    }
                    continue;
                }
                index++;
            }
        }
        else{
            tokens.Add(new Token(code[index].ToString(), index, index+1, TokenType.Punctuation));
            index++;
            goto loop;
        }
    }
}

static class Parser{
    static List<List<Token>> SplitByComma(List<Token> tokens){
        List<List<Token>> output = [];
        var start = 0;
        for(var i=0;i<tokens.Count;i++){
            if(tokens[i].value == ","){
                output.Add(tokens[start..i]);
                start = i+1;
            }
        }
        if(start<tokens.Count){
            output.Add(tokens[start..tokens.Count]);
        }
        return output;
    }

    static Instruction[] ParseExpressionInParens(Token token){
        return ParseExpression(Tokenizer.Tokenize(token.value));
    }

    public static Instruction[] ParseExpression(List<Token> tokens){
        var operators = new string[][]{["<", ">"], ["+", "-"], ["/", "*"]};
        if(tokens.Count == 0){
            throw new Exception("No tokens");
        }
        else if(tokens.Count == 1){
            if(tokens[0].type == TokenType.Parens){
                return ParseExpressionInParens(tokens[0]);
            }
            else if(tokens[0].type == TokenType.Float){
                return [new Instruction(JOpcode.F32Const, tokens[0].value)];
            }
            else if(tokens[0].type == TokenType.Int){
                return [new Instruction(JOpcode.I32Const, tokens[0].value)];
            }
            else if(tokens[0].type == TokenType.Varname){
                return [new Instruction(JOpcode.GetLocal, tokens[0].value)];
            }
            else{
                throw new Exception("Unexpected token: "+tokens[0]);
            }
        }
        else if(tokens.Count == 2){
            if(tokens[0].type == TokenType.Varname && tokens[1].type == TokenType.Parens){
                var args = SplitByComma(Tokenizer.Tokenize(tokens[1].value)).Select(ParseExpression).ToArray();
                return [.. args.SelectMany(a=>a).ToArray(), new Instruction(JOpcode.Call, tokens[0].value)];
            }
        }
        foreach(var ops in operators){
            var index = tokens.FindLastIndex(t=>ops.Contains(t.value));
            if(index>=0){
                var left = ParseExpression(tokens[0..index]);
                var right = ParseExpression(tokens[(index+1)..tokens.Count]);
                return [..left, ..right, new Instruction(JOpcode.BinaryOp, tokens[index].value)];
            }
        }
        throw new Exception("Unexpected tokens");
    }

    static Instruction[] ParseBody(string code, List<string> blocks, ref int labelID){
        var tokens = Tokenizer.Tokenize(code);
        List<Instruction> instructions = [];
        var i = 0;
        while(true){
            if(i>=tokens.Count){
                return [..instructions];
            }
            if(i+1<tokens.Count && tokens[i+1].value == "="){
                var end = tokens.FindIndex(i+2, t=>t.value == ";");
                if(end<0){
                    throw new Exception("No end of expression");
                }
                instructions.AddRange(ParseExpression(tokens[(i+2)..end]));
                instructions.Add(new Instruction(JOpcode.SetLocal, tokens[i].value));
                i = end+1;
            }
            else if(tokens[i].value == "return"){
                var end = tokens.FindIndex(i+1, t=>t.value == ";");
                if(end<0){
                    throw new Exception("No end of expression");
                }
                instructions.AddRange(ParseExpression(tokens[(i+1)..end]));
                i = end+1;
            }
            else if(tokens[i].value == "if"){
                instructions.AddRange(ParseExpressionInParens(tokens[i+1]));
                instructions.Add(new Instruction(JOpcode.UnaryOp, "!"));
                instructions.Add(new Instruction(JOpcode.GotoIf, labelID.ToString()));
                instructions.AddRange(ParseBody(tokens[i+2].value, blocks, ref labelID));
                instructions.Add(new Instruction(JOpcode.Label, labelID.ToString()));
                labelID++;
                i+=3;
            }
            else if(tokens[i].value == "break"){
                instructions.Add(new Instruction(JOpcode.Goto, blocks[^1]));
                i+=2;
            }
            else if(tokens[i] .value== "while"){
                var startLabelID = labelID.ToString();
                var endLabelID = (labelID+1).ToString();
                labelID+=2;
                instructions.Add(new Instruction(JOpcode.Label, startLabelID));
                instructions.AddRange(ParseExpressionInParens(tokens[i+1]));
                instructions.Add(new Instruction(JOpcode.UnaryOp, "!"));
                instructions.Add(new Instruction(JOpcode.GotoIf, endLabelID));
                blocks.Add(endLabelID);
                instructions.AddRange(ParseBody(tokens[i+2].value, blocks, ref labelID));
                blocks.RemoveAt(blocks.Count-1);
                instructions.Add(new Instruction(JOpcode.Goto, startLabelID));
                instructions.Add(new Instruction(JOpcode.Label, endLabelID));
                i+=3;
            }
            else if(tokens[i].value == "var"){
                var name = tokens[i+1].value;
                var end = tokens.FindIndex(i+3, t=>t.value == ";");
                if(end<0){
                    throw new Exception("No end of expression");
                }
                var expr = ParseExpression(tokens[(i+3)..end]);
                instructions.AddRange(expr);
                instructions.Add(new Instruction(JOpcode.CreateLocal, name));
                i = end+1;
            }
            else{
                var end = tokens.FindIndex(i, t=>t.value == ";");
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
        var parameterTokens = SplitByComma(Tokenizer.Tokenize(code));
        return parameterTokens.Select(p=>new Parameter(p[0].value, p[1].value)).ToArray();
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
            var parameters = ParseParameters(tokens[i+2].value);
            var body = ParseBody(tokens[i+3].value);
            functions.Add(new Function(returnType.value, name.value, parameters, body));
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