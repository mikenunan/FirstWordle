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

let updateCountsForLetter (counts:Map<char,int>) letter =
    counts.Add(letter, counts[letter] + 1)

let updateCounts (word:string) (counts:Map<char,int>) =
    word.ToCharArray() |> Array.fold updateCountsForLetter counts

let calculateLetterFrequencies words =
    let counts = seq { 'A' .. 'Z' } |> Seq.map (fun letter -> (letter, 0)) |> Map.ofSeq
    List.fold (fun counts word -> updateCounts word counts) counts words
    
let computeWordScore (word:string) (letterCounts:Map<char,int>) =
    word.ToCharArray() |> Array.fold (fun score ch -> score + letterCounts[ch]) 0

let calculateWordScores allCandidateWords letterCounts =
    allCandidateWords |> Seq.map (fun word -> (word, computeWordScore word letterCounts)) |> Map.ofSeq

let processDictionaryFromUrlAsync url wordsToPrint =
    async {
        let! allCandidateWords = GetCandidateWordsAsync url filterToCandidates
        let letterCounts = calculateLetterFrequencies allCandidateWords
        let candidateWordsWithoutDuplicateLetters =
            allCandidateWords
                |> Seq.filter (fun word -> word.ToCharArray() |> Seq.distinct |> Seq.length > 4)
        let wordScores = calculateWordScores candidateWordsWithoutDuplicateLetters letterCounts
        let maxScore = (wordScores |> Seq.maxBy (fun pair -> pair.Value)).Value
        Console.WriteLine($"Max score is {maxScore}, top ten words are:")
        let sortedWordScores = wordScores |> Seq.sortByDescending (fun pair -> pair.Value)
        sortedWordScores |> Seq.take 10 |> Seq.iter Console.WriteLine
        wordsToPrint
            |> Seq.iter
                (fun (wordAnyCase:string) ->
                    let word = wordAnyCase.ToUpperInvariant()
                    let rankingOption = sortedWordScores |> Seq.tryFindIndex (fun pair -> pair.Key = word)
                    let message =
                        match rankingOption with
                            | Some(ranking) -> $"{word} scores {wordScores[word]}, ranks at position {ranking} out of {wordScores.Count}"
                            | None -> $"No match for word {word} (out of {wordScores.Count} possibilities)"
                    Console.WriteLine(message))
        Console.WriteLine("Overall letter counts across all candidate words:")
        letterCounts
            |> Seq.sortByDescending (fun pair -> pair.Value)
            |> Seq.iter (fun pair -> Console.WriteLine($"{pair.Key}, {pair.Value}"))
    }

[<EntryPoint>]
let main argv =
    processDictionaryFromUrlAsync dictionaryUrl argv |> Async.RunSynchronously
    0
