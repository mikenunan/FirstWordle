# FirstWordle

Find optimal first word choice for the fun word game https://www.powerlanguage.co.uk/wordle/

This repo holds the source for a simple .NET console app written in F#. A few days ago a friend pointed me towards Josh Wardle's nearly-eponymous and very excellent word game, Wordle. In this game you have to guess a five-letter word, with the first turn being completely blind. It occurred to me that a word containing the maximally common combination of five different letters would be the optimal first word to play. This gives the best chance of hits matching the target word for the puzzle.

The code here downloads the current latest `en-US` dictionary from [wooorm](https://raw.githubusercontent.com/wooorm/dictionaries/main/dictionaries/en/index.dic) and filters it to leave just five-letter words containing only alphabetic characters. It computes character counts for all 26 letters across that candidate word collection, then allocates scores to the subset of those words that have no repeated letters. The score is simply the sum of the character count values for each letter.

It then prints the top ten, together with a summary of the overall character counts. I'd expected that there would be multiple words in equal top position, but as it turns out there is a single outright winner. [Click here to see example output](https://github.com/mikenunan/FirstWordle/blob/master/sample-output.txt) if you want the spoiler. Second and third positions _are_ held jointly though (by four words and two words respectively).

However, looking now I see [quite](https://matt-rickard.com/wordle-whats-the-best-starting-word/) a [few](https://screenrant.com/wordle-best-words-start-with-strategy-guesses/) other [people](https://bert.org/2021/11/24/the-best-starting-word-in-wordle/) have addressed this problem similarly which is no big surprise, but it *is* surprising that the word they have found is an archaic term (not in the wooorm dictionary) which is an anagram of the very ordinary word that my program puts top!

Finally, a tip of the hat to Tyler Glaiel for this [altogether deeper analysis](https://medium.com/@tglaiel/the-mathematically-optimal-first-guess-in-wordle-cbcb03c19b0a) of the problem, which shows that neither "my" word nor it's archaic anagram are actually the best first word to play. Well, at least I got the chance to write some F# code for a change lol.
