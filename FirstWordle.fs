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

let filterAndProcessLinesFromUrlDownloadAsync (url:string) (filterToCandidates:(AsyncSeq<string> -> AsyncSeq<string>)) processLine =
    async {
        use client = new HttpClient()
        use! response = client.GetAsync(url) |> Async.AwaitTask
        use content = response.Content
        use stream = content.ReadAsStream()
        use reader = new StreamReader(stream)
        let lines = reader |> readLines
        let! filterAndProcess =
            filterToCandidates lines
                |> AsyncSeq.take(100)
                |> AsyncSeq.iter processLine
                |> Async.StartChild
        do! filterAndProcess
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

let filterAndProcessLinesFromUrlAsync url =
    async {
        let! asyncJob = filterAndProcessLinesFromUrlDownloadAsync url filterToCandidates Console.WriteLine |> Async.StartChild
        do! asyncJob
    }

[<EntryPoint>]
let main argv =
    filterAndProcessLinesFromUrlAsync dictionaryUrl |> Async.RunSynchronously
    0
