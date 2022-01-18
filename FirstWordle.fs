open FSharp.Control
open FSharp.Collections
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
    Set.fold (fun counts word -> updateCounts word counts) counts words
    
let hasNoDuplicateLetters (word:string) =
    word.ToCharArray() |> Seq.distinct |> Seq.length > 4

let computeWordScore (word:string) (letterCounts:Map<char,int>) =
    word.ToCharArray() |> Array.fold (fun score ch -> score + letterCounts[ch]) 0

let calculateWordScores words letterCounts =
    words |> Seq.map (fun word -> (word, computeWordScore word letterCounts)) |> Map.ofSeq

let processPlayableWordListFromAsync wordsToPrint =
    async {
        let! possibleGuessesList = GetFilteredWordsAsync "wordlist_guesses.txt"
        let possibleGuesses = possibleGuessesList |> Set.ofSeq
        let guessesWithoutDuplicateLetters =
            possibleGuesses
                |> Seq.filter hasNoDuplicateLetters
                |> Seq.toList
        let! possibleSolutionWords = GetFilteredWordsAsync "wordlist_solutions.txt"
        let solutionWordsWithoutDuplicateLetters =
            possibleSolutionWords
                |> Seq.filter hasNoDuplicateLetters
                |> Seq.toList
        let nonPlayableSolutionWords =
            possibleSolutionWords
                |> Seq.except possibleGuesses
                |> Seq.toList
        if nonPlayableSolutionWords.Length > 0 then failwith "Non-playable solution words should not exist"
        let letterCounts = calculateLetterFrequencies possibleGuesses
        let wordScores = calculateWordScores guessesWithoutDuplicateLetters letterCounts
        let maxScore = (wordScores |> Seq.maxBy (fun pair -> pair.Value)).Value
        let sortedWordScores =
            wordScores
                |> Seq.sortByDescending (fun pair -> pair.Value)
        Console.WriteLine()
        Console.Write($"There are {possibleGuesses.Count} playable words (all of them are solution words)")
        Console.WriteLine($" of which {guessesWithoutDuplicateLetters.Length} have no duplicate letters,")
        Console.Write($"and of {possibleSolutionWords.Length} possible solution words")
        Console.WriteLine($" {solutionWordsWithoutDuplicateLetters.Length} have no duplicate letters")
        Console.WriteLine()
        Console.WriteLine($"Among first word choices the max score is {maxScore}, here's the top ten:")
        sortedWordScores
            |> Seq.take 10
            |> Seq.iter Console.WriteLine
        Console.WriteLine()
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
        Console.WriteLine()
        Console.WriteLine("Overall letter counts across all candidate words:")
        letterCounts
            |> Seq.sortByDescending (fun pair -> pair.Value)
            |> Seq.iter (fun pair -> Console.WriteLine($"{pair.Key}, {pair.Value}"))
        Console.WriteLine()
    }

[<EntryPoint>]
let main argv =
    processPlayableWordListFromAsync argv |> Async.RunSynchronously
    0
