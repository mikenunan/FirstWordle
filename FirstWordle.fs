open FSharp.Control
open System
open System.IO
open System.Net.Http

let playableWordListUrl = "https://raw.githubusercontent.com/TylerGlaiel/wordlebot/main/wordlist_guesses.txt"

let readLines (reader:StreamReader) =
    asyncSeq {
        while not reader.EndOfStream do
            yield reader.ReadLine()
    }

let GetFilteredWordsAsync (url:string) (filter:(AsyncSeq<string> -> AsyncSeq<string>)) =
    async {
        use client = new HttpClient()
        use! response = client.GetAsync(url) |> Async.AwaitTask
        use content = response.Content
        use stream = content.ReadAsStream()
        use reader = new StreamReader(stream)
        return reader |> readLines |> filter |> AsyncSeq.toListSynchronously
    }

let isCandidateWord (word:string) =
    word.Length = 5 &&
    word.ToCharArray() |> Seq.forall (fun ch -> ch >= 'A' && ch <= 'Z')

let filterToCandidates (lines:AsyncSeq<string>) =
    lines
        |> AsyncSeq.map (fun word -> word.ToUpperInvariant())
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

let processPlayableWordListFromUrlAsync url wordsToPrint =
    async {
        let! allCandidateWords = GetFilteredWordsAsync url filterToCandidates
        let letterCounts = calculateLetterFrequencies allCandidateWords
        let candidateWordsWithoutDuplicateLetters =
            allCandidateWords
                |> Seq.filter (fun word -> word.ToCharArray() |> Seq.distinct |> Seq.length > 4)
                |> Seq.toList
        Console.WriteLine($"There are {allCandidateWords.Length} playable words, of which {candidateWordsWithoutDuplicateLetters.Length} have no duplicate letters")
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
                            | None -> $"{word} scores {computeWordScore word letterCounts}, but no match for word {word} in ranking"
                    Console.WriteLine(message))
        Console.WriteLine("Overall letter counts across all candidate words:")
        letterCounts
            |> Seq.sortByDescending (fun pair -> pair.Value)
            |> Seq.iter (fun pair -> Console.WriteLine($"{pair.Key}, {pair.Value}"))
    }

[<EntryPoint>]
let main argv =
    processPlayableWordListFromUrlAsync playableWordListUrl argv |> Async.RunSynchronously
    0
