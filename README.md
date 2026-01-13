# *5Letters* Game Bot

*5Letters* (russian: *5букв*) game and its rules can be found [here](https://5bukv.tinkoff.ru).

# Installation

1. Install dotnet SDK 10.0. Follow the [instructions](https://dotnet.microsoft.com/en-us/download).
1. [Download](https://github.com/cwaterloo/5bukv/archive/refs/heads/main.zip) this repository.
1. Unpack the downloaded zip-archive.
1. Open *Command Prompt* (Windows) or *Terminal* (Unix) and proceed to the directory with unpacked 
   data. Thus, the current directory should contain `russian5.txt`. Use `cd` command to change directory.
1. Execute the following command to compile the project.

```shell
dotnet build -c Release
```

You should see *Build succeeded.*

# Quick Start

## Graph Preparation

In order to use the application you need to convert the set of dictionaries into a graph.
The more vocabularies you use the more universal graph is obtained. For example, if you would like to play
5Bukv (T-Bank) and Wordly you need to provide 5Bukv (T-Bank) and Wordly dictionaries. Beware that only
the common words (belongs to all the provided dictionaries) can "attack".

Execute the following command to start the process. You can any number of dictionaries (the last parameter).

```shell
5LettersBin/bin/Release/net10.0/5LettersBin graph false path_to_output_graph.pb.gz russian5_wordly.txt russian5_tbank.txt
```

Note: you might not want to waste the time to find the first candidate(s). Replace the `[]` in
[ConsoleApp.cs](https://github.com/cwaterloo/5bukv/blob/main/5LettersLib/ConsoleApp.cs#L186) with either and build the project again.

* `["колит", "серна"]` (for russian words, two first candidates)
* `["норка"]` (for russian words, one first candidate)
* `["lions", "caret"]` (for english words, two first candidates)
* `["lares"]` (for english words, one first candidate)

## Interactive mode (Bot)

Execute the following command to start the bot.

```shell
5LettersBin/bin/Release/net10.0/5LettersBin interactive path_to_graph.pb.gz
```

It works instantly. No waiting time.

## Collect Statistics

This mode simulates playing the game. The application picks up each word from dictionary one by one
and starts the game. Once all the rounds completed it shows the collected statistics.

Execute the following command to start.

```shell
5LettersBin/bin/Release/net10.0/5LettersBin stats path_to_graph.pb.gz
```

It works instantly. No waiting time.

## Help

Just execute the following command.

```shell
5LettersBin/bin/Release/net10.0/5LettersBin
```

Enjoy!
