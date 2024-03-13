


enum Opcode{I32Const, F32Const, GetLocal, Op, SetLocal, Call}
class Instruction(Opcode opcode, string value){
    public Opcode opcode = opcode;
    public string value = value;

    public override string ToString()
    {
        return "(" + opcode.ToString() + " " + value + ")";
    }
}

class VM(Instruction[] instructions){
    readonly Instruction[] instructions = instructions;

    public dynamic Run(){
        var index = 0;
        Stack<dynamic> stack = [];
        Dictionary<string, dynamic> locals = [];

        while(true){
            if(index>=instructions.Length){
                return stack.Pop();
            }
            var i = instructions[index];
            if(i.opcode == Opcode.I32Const){
                stack.Push(int.Parse(i.value));
            }
            if(i.opcode == Opcode.F32Const){
                stack.Push(float.Parse(i.value));
            }
            else if(i.opcode == Opcode.GetLocal){
                stack.Push(locals[i.value]);
            }
            else if(i.opcode == Opcode.SetLocal){
                if(!locals.ContainsKey(i.value)){
                    locals[i.value] = stack.Pop();
                }
                else{
                    locals.Add(i.value, stack.Pop());
                }
            }
            else if(i.opcode == Opcode.Call){
                if(i.value == "Print"){
                    Console.WriteLine(stack.Pop());
                }
                else{
                    throw new Exception("Unexpected function");
                }
            }
            else if(i.opcode == Opcode.Op){
                var right = stack.Pop();
                var left = stack.Pop();
                var op = i.value;
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
            }
            index++;
        }
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
        var punctuation = ";+-*/=";
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
                depth++;
                split = false;
                start = index;
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

    public static Instruction[] ParseExpression(List<string> tokens){
        var operators = new string[][]{["+", "-"], ["/", "*"]};
        if(tokens.Count == 0){
            throw new Exception("No tokens");
        }
        else if(tokens.Count == 1){
            if(tokens[0][0] == '('){
                return ParseExpression(Tokenizer.Tokenize(tokens[0][1..^1]));
            }
            else if(char.IsDigit(tokens[0][0])){
                if(tokens[0].Contains('.')){
                    return [new Instruction(Opcode.F32Const, tokens[0])];
                }
                return [new Instruction(Opcode.I32Const, tokens[0])];
            }
            else if(char.IsLetter(tokens[0][0])){
                return [new Instruction(Opcode.GetLocal, tokens[0])];
            }
            else{
                throw new Exception("Unexpected token: "+tokens[0]);
            }
        }
        else if(tokens.Count == 2){
            if(char.IsLetter(tokens[0][0]) && tokens[1][0] == '('){
                var args = SplitByComma(Tokenizer.Tokenize(tokens[1][1..^1])).Select(ParseExpression).ToArray();
                return [.. args.SelectMany(a=>a).ToArray(), new Instruction(Opcode.Call, tokens[0])];
            }
        }
        foreach(var ops in operators){
            var index = tokens.FindLastIndex(t=>ops.Contains(t));
            if(index>=0){
                var left = ParseExpression(tokens[0..index]);
                var right = ParseExpression(tokens[(index+1)..tokens.Count]);
                return [..left, ..right, new Instruction(Opcode.Op, tokens[index])];
            }
        }
        foreach(var t in tokens){
            Console.WriteLine(t);
        }
        throw new Exception("Unexpected tokens");
    }

    public static Instruction[] Parse(List<string> tokens){
        List<Instruction> instructions = [];
        var i = 0;
        while(true){
            if(i>=tokens.Count){
                return [..instructions];
            }
            if(tokens[i] == "return"){
                var end = tokens.IndexOf(";", i+1);
                if(end<0){
                    throw new Exception("No end of expression");
                }
                instructions.AddRange(ParseExpression(tokens[(i+1)..end]));
                i = end+1;
            }
            else if(tokens[i] == "var"){
                var name = tokens[i+1];
                var end = tokens.IndexOf(";", i+3);
                if(end<0){
                    throw new Exception("No end of expression");
                }
                var expr = ParseExpression(tokens[(i+3)..end]);
                instructions.AddRange(expr);
                instructions.Add(new Instruction(Opcode.SetLocal, name));
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
}

class Program{
    static void Main(){
        var tokens = Tokenizer.Tokenize(@"
        var x = 5 + 5.3;
        Print(x * 2);
        Print(x * 3);
        return 2 * (x - 2) - 6;");
        var instructions = Parser.Parse(tokens);
        var vm = new VM(instructions);
        Console.WriteLine(vm.Run());
    }
}