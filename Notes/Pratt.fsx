open System

type Token =
   | Atom of string
   | Op of char
   | EOF

type Lexer = Lexer of Ref<int> * array<Token>

let isOperator (c : char) =
    ['+'; '-'; '*'; '/']
    |> List.contains c
let newLexer (input: string) : Lexer =
    let mutable i = 0
    let rec tokenize () =
        if i >= input.Length then
            []
        else
            let c = input[i]
            if System.Char.IsWhiteSpace(c) then
                i <- i + 1
                tokenize ()
            elif System.Char.IsDigit(c) then
                let start = i
                while i < input.Length && System.Char.IsDigit(input[i]) do
                    i <- i + 1
                Atom(input.Substring(start, i - start)) :: tokenize ()
            elif isOperator c then
                i <- i + 1
                Op(c) :: tokenize ()
            else
                failwith "Invalid character"
    tokenize () |> List.toArray |> (fun toks -> Lexer(ref(0), toks))

let nextTok (lexer : Lexer) = 
    match lexer with
    | Lexer(r, toks) ->
    let index = r.Value
    r.Value <- index + 1
    if index < toks.Length
    then toks[index]
    else EOF

let peekTok (lexer : Lexer) =
    match lexer with
    | Lexer(r, toks) ->
    let index = r.Value
    if index < toks.Length
    then toks[index]
    else EOF

type S =
   | SAtom of string
   | SCons of char * array<S>

let (|Add|Mul|Sub|Div|) op =
    if op = '+'
    then Add
    else if op = '*'
    then Mul
    else if op = '-'
    then Sub
    else if op = '/'
    then Div
    else failwith "unkonwn operator"

let infix_binding_power (op : char) = 
    match op with
    | Add | Sub -> (1,2)
    | Mul | Div -> (3,4)
let rec expr_bp lexer min_bp =
    let mutable lhs = match nextTok lexer with
                        | Atom(s) -> SAtom s
                        | _ -> failwith "bad token"
    
    let mutable continueLoop = true
    while continueLoop do
        match peekTok lexer with
        | EOF -> continueLoop <- false
        | Op(op) -> 
            let (l_bp, r_bp) = infix_binding_power op
            if l_bp < min_bp
            then continueLoop <- false
            else
              nextTok lexer |> ignore
              let rhs = expr_bp lexer r_bp
              lhs <- SCons(op, [|lhs; rhs|])
        | _ -> failwith "bad token"
    lhs

let expr str = 
    let lexer = newLexer str
    expr_bp lexer 0

printfn "%A" (expr "1 + 2 * 3 / 4")