


enum Opcode{I32Const, F32Const, GetLocal, BinaryOp, UnaryOp, SetLocal, CreateLocal, Call, GotoIf, Label, Goto}
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
        Dictionary<string, int> labels = [];

        for(var i=0;i<instructions.Length;i++){
            if(instructions[i].opcode == Opcode.Label){
                labels.Add(instructions[i].value, i);
            }
        }
        while(true){
            if(index>=instructions.Length){
                return stack.Pop();
            }
            var instr = instructions[index];
            if(instr.opcode == Opcode.Label){}
            else if(instr.opcode == Opcode.GotoIf){
                if(stack.Pop()){
                    index = labels[instr.value];
                }
            }
            else if(instr.opcode == Opcode.Goto){
                index = labels[instr.value];
            }
            else if(instr.opcode == Opcode.I32Const){
                stack.Push(int.Parse(instr.value));
            }
            if(instr.opcode == Opcode.F32Const){
                stack.Push(float.Parse(instr.value));
            }
            else if(instr.opcode == Opcode.GetLocal){
                stack.Push(locals[instr.value]);
            }
            else if(instr.opcode == Opcode.SetLocal){
                locals[instr.value] = stack.Pop();
            }
            else if(instr.opcode == Opcode.CreateLocal){
                locals.Add(instr.value, stack.Pop());
            }
            else if(instr.opcode == Opcode.Call){
                if(instr.value == "Print"){
                    Console.WriteLine(stack.Pop());
                }
                else{
                    throw new Exception("Unexpected function");
                }
            }
            else if(instr.opcode == Opcode.UnaryOp){
                var value = stack.Pop();
                var op = instr.value;
                if(op == "!"){
                    stack.Push(!value);
                }
                else{
                    throw new Exception("Unexpected UnaryOp: "+op);
                }
            }
            else if(instr.opcode == Opcode.BinaryOp){
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

class Parser{
    List<string> blocks = [];
    int labelID = 0;

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

    static Instruction[] ParseExpressionInParens(string group){
        return ParseExpression(Tokenizer.Tokenize(group[1..^1]));
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
                return [..left, ..right, new Instruction(Opcode.BinaryOp, tokens[index])];
            }
        }
        throw new Exception("Unexpected tokens");
    }

    public Instruction[] ParseBody(string group){
        var tokens = Tokenizer.Tokenize(group[1..^1]);
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
                instructions.Add(new Instruction(Opcode.SetLocal, tokens[i]));
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
                instructions.Add(new Instruction(Opcode.UnaryOp, "!"));
                instructions.Add(new Instruction(Opcode.GotoIf, labelID.ToString()));
                instructions.AddRange(ParseBody(tokens[i+2]));
                instructions.Add(new Instruction(Opcode.Label, labelID.ToString()));
                labelID++;
                i+=3;
            }
            else if(tokens[i] == "break"){
                instructions.Add(new Instruction(Opcode.Goto, blocks[^1]));
                i+=2;
            }
            else if(tokens[i] == "while"){
                var startLabelID = labelID.ToString();
                var endLabelID = (labelID+1).ToString();
                labelID+=2;
                instructions.Add(new Instruction(Opcode.Label, startLabelID));
                instructions.AddRange(ParseExpressionInParens(tokens[i+1]));
                instructions.Add(new Instruction(Opcode.UnaryOp, "!"));
                instructions.Add(new Instruction(Opcode.GotoIf, endLabelID));
                blocks.Add(endLabelID);
                instructions.AddRange(ParseBody(tokens[i+2]));
                blocks.RemoveAt(blocks.Count-1);
                instructions.Add(new Instruction(Opcode.Goto, startLabelID));
                instructions.Add(new Instruction(Opcode.Label, endLabelID));
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
                instructions.Add(new Instruction(Opcode.CreateLocal, name));
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
        var code = @"
        {
            var i = 0;
            while(i<5){
                Print(i);
                i=i+1;
            }
            while(i<100){
                Print(i);
                if(i>10){
                    break;
                }
                i=i+2;
            }
            return i;
        }";
        var instructions = new Parser().ParseBody(Tokenizer.Tokenize(code)[0]);
        var vm = new VM(instructions);
        Console.WriteLine(vm.Run());
    }
}