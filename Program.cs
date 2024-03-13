
enum Opcode{Get, Op}
class Instruction(Opcode opcode, string value){
    public Opcode opcode = opcode;
    public string value = value;

    public override string ToString()
    {
        return "("+opcode.ToString() + " "+value+")";
    }
}

class VM(Instruction[] instructions){
    Instruction[] instructions = instructions;

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
    public List<string> tokens = [];

    void AddPrevToken(){
        if(!split){
            tokens.Add(code[start..index]);
        }
        split = true;
    }

    public void Tokenize(){
        var punctuation = "{}();+-*/";

        for(index=0;index<code.Length;index++){
            var c = code[index];
            if(char.IsWhiteSpace(c)){
                AddPrevToken();
            }
            else if(punctuation.Contains(c)){
                AddPrevToken();
                tokens.Add(code.Substring(index, 1));
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
}

static class Parser{

    public static Instruction[] Parse(List<string> tokens){
        var operators = new string[][]{["+", "-"], ["/", "*"]};
        if(tokens.Count == 1){
            return [new Instruction(Opcode.Get, tokens[0])];
        }
        foreach(var ops in operators){
            var index = tokens.FindLastIndex(t=>ops.Contains(t));
            if(index>=0){
                var left = Parse(tokens[0..index]);
                var right = Parse(tokens[(index+1)..tokens.Count]);
                return [..left, ..right, new Instruction(Opcode.Op, tokens[index])];
            }
        }
        throw new Exception("Unexpected tokens");
    }
}

class Program{
    static void Main(){
        var tokenizer = new Tokenizer("2 * 22 + 5");
        tokenizer.Tokenize();
        var instructions = Parser.Parse(tokenizer.tokens);
        var vm = new VM(instructions);
        Console.WriteLine(vm.Run());
    }
}