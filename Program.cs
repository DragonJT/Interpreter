
enum Opcode{Get, Op}
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

        while(true){
            if(index>=instructions.Length){
                return stack.Pop();
            }
            if(instructions[index].opcode == Opcode.Get){
                stack.Push(int.Parse(instructions[index].value));
            }
            else if(instructions[index].opcode == Opcode.Op){
                var right = stack.Pop();
                var left = stack.Pop();
                var op = instructions[index].value;
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
        index+=delta;
    }

    public void Tokenize(){
        var punctuation = ";+-*/";
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
                    tokens.Add(code.Substring(index, 1));
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

    public static Instruction[] ParseExpression(List<string> tokens){
        var operators = new string[][]{["+", "-"], ["/", "*"]};
        if(tokens.Count == 1){
            if(tokens[0][0] == '('){
                return ParseExpression(Tokenizer.Tokenize(tokens[0][1..^1]));
            }
            return [new Instruction(Opcode.Get, tokens[0])];
        }
        foreach(var ops in operators){
            var index = tokens.FindLastIndex(t=>ops.Contains(t));
            if(index>=0){
                var left = ParseExpression(tokens[0..index]);
                var right = ParseExpression(tokens[(index+1)..tokens.Count]);
                return [..left, ..right, new Instruction(Opcode.Op, tokens[index])];
            }
        }
        throw new Exception("Unexpected tokens");
    }
}

class Program{
    static void Main(){
        var tokens = Tokenizer.Tokenize("2 * (22 + 5) - 6");
        var instructions = Parser.ParseExpression(tokens);
        var vm = new VM(instructions);
        Console.WriteLine(vm.Run());
    }
}