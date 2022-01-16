open FSharp.Control
open System
open System.IO
open System.Net.Http

let dictionaryUrl = "https://raw.githubusercontent.com/wooorm/dictionaries/main/dictionaries/en/index.dic"

let readLines (reader:StreamReader) =
    asyncSeq {
        while not reader.EndOfStream do
            yield reader.ReadLine()
    }

let GetCandidateWordsAsync (url:string) (filterToCandidates:(AsyncSeq<string> -> AsyncSeq<string>)) =
    async {
        use client = new HttpClient()
        use! response = client.GetAsync(url) |> Async.AwaitTask
        use content = response.Content
        use stream = content.ReadAsStream()
        use reader = new StreamReader(stream)
        let lines = reader |> readLines
        let filtered = filterToCandidates lines
        return filtered |> AsyncSeq.toListSynchronously
    }

let rawLineToWord (rawLine:string) =
    let trimmedLine = rawLine.ToUpperInvariant().Trim()
    let slashIndex = trimmedLine.IndexOf('/')
    if slashIndex < 0
        then trimmedLine
        else trimmedLine.Substring(0, slashIndex)

let isCandidateWord (word:string) =
    word.Length = 5 &&
    word.ToCharArray() |> Seq.forall (fun ch -> ch >= 'A' && ch <= 'Z')

let filterToCandidates (lines:AsyncSeq<string>) =
    lines
        |> AsyncSeq.map rawLineToWord
        |> AsyncSeq.filter isCandidateWord

let updateCountsForLetter (counts:Map<char,int>) (letter:char) =
    counts.Add(letter, counts[letter] + 1)

let updateCounts (word:string) (counts:Map<char,int>) =
    word.ToCharArray() |> Array.fold updateCountsForLetter counts

let calculateLetterFrequencies (words:string list) =
    let counts = seq { 'A' .. 'Z' } |> Seq.map (fun letter -> (letter, 0)) |> Map.ofSeq
    List.fold (fun counts word -> updateCounts word counts) counts words
    
let processDictionaryFromUrlAsync url =
    async {
        let! allCandidateWords = GetCandidateWordsAsync url filterToCandidates
        let letterCounts = calculateLetterFrequencies allCandidateWords
        letterCounts |> Console.WriteLine
    }

[<EntryPoint>]
let main argv =
    processDictionaryFromUrlAsync dictionaryUrl |> Async.RunSynchronously
    0
