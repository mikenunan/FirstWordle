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

let filterAndProcessLinesFromUrlDownloadAsync (url:string) (filterLines:(AsyncSeq<string> -> AsyncSeq<string>)) processLine =
    async {
        use client = new HttpClient()
        use! response = client.GetAsync(url) |> Async.AwaitTask
        use content = response.Content
        use stream = content.ReadAsStream()
        use reader = new StreamReader(stream)
        let lines = reader |> readLines
        let! filterAndProcess =
            filterLines lines
                |> AsyncSeq.iter processLine
                |> Async.StartChild
        do! filterAndProcess
    }

let filterLines (lines:AsyncSeq<string>) =
    lines |> AsyncSeq.take(100)

let filterAndProcessLinesFromUrlAsync url =
    async {
        let! x = filterAndProcessLinesFromUrlDownloadAsync url filterLines Console.WriteLine |> Async.StartChild
        do! x
    }

[<EntryPoint>]
let main argv =
    filterAndProcessLinesFromUrlAsync dictionaryUrl |> Async.RunSynchronously
    0
