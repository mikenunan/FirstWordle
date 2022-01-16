open System.Net
open System
open System.IO

let fetchUrl readHtml url =
    let request = WebRequest.Create(Uri(url))
    use response = request.GetResponse()
    use stream = response.GetResponseStream()
    use reader = new StreamReader(stream)
    readHtml reader url

let readHtml (reader:StreamReader) url = 
    let html = reader.ReadToEnd()
    let html1000 = html.Substring(0,1000)
    printfn "Downloaded %s. First 1000 is %s" url html1000
    html

let dictionary = fetchUrl readHtml "https://raw.githubusercontent.com/wooorm/dictionaries/main/dictionaries/en/index.dic"
