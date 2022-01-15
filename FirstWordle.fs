open System.Net
open System
open System.IO

// Code from https://fsharpforfunandprofit.com/posts/fvsc-download/

// Fetch the contents of a web page
let fetchUrl callback url =
    let req = WebRequest.Create(Uri(url))
    use resp = req.GetResponse()
    use stream = resp.GetResponseStream()
    use reader = new IO.StreamReader(stream)
    callback reader url

let myCallback (reader:IO.StreamReader) url = 
    let html = reader.ReadToEnd()
    let html1000 = html.Substring(0,1000)
    printfn "Downloaded %s. First 1000 is %s" url html1000
    html      // return all the html

//test
let dictionary = fetchUrl myCallback "https://raw.githubusercontent.com/wooorm/dictionaries/main/dictionaries/en/index.dic"
