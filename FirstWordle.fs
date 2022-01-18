open FSharp.Control
open System
open System.IO

let readLines (reader:StreamReader) =
    asyncSeq {
        while not reader.EndOfStream do
            yield reader.ReadLine().ToUpperInvariant()
    }

let GetFilteredWordsAsync (path:string) =
    async {
        use stream = new FileStream(path, FileMode.Open)
        use reader = new StreamReader(stream)
        return reader |> readLines |> AsyncSeq.toListSynchronously
    }

let updateCountsForLetter (counts:Map<char,int>) letter =
    counts.Add(letter, counts[letter] + 1)

let updateCounts (word:string) (counts:Map<char,int>) =
    word.ToCharArray() |> Array.fold updateCountsForLetter counts

let calculateLetterFrequencies words =
    let counts = seq { 'A' .. 'Z' } |> Seq.map (fun letter -> (letter, 0)) |> Map.ofSeq
    List.fold (fun counts word -> updateCounts word counts) counts words
    
let computeWordScore (word:string) (letterCounts:Map<char,int>) =
    word.ToCharArray() |> Array.fold (fun score ch -> score + letterCounts[ch]) 0

let calculateWordScores words letterCounts =
    words |> Seq.map (fun word -> (word, computeWordScore word letterCounts)) |> Map.ofSeq

let processPlayableWordListFromAsync wordsToPrint =
    async {
        let! possibleGuesses = GetFilteredWordsAsync "wordlist_guesses.txt"
        let letterCounts = calculateLetterFrequencies possibleGuesses
        let possibleGuessesWithoutDuplicateLetters =
            possibleGuesses
                |> Seq.filter (fun word -> word.ToCharArray() |> Seq.distinct |> Seq.length > 4)
                |> Seq.toList
        Console.WriteLine($"There are {possibleGuesses.Length} playable words, of which {possibleGuessesWithoutDuplicateLetters.Length} have no duplicate letters")
        let wordScores = calculateWordScores possibleGuessesWithoutDuplicateLetters letterCounts
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
    processPlayableWordListFromAsync argv |> Async.RunSynchronously
    0
