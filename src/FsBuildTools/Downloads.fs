namespace FsBuildTools

open Fake.IO

module Downloads =
    let private ensure7zip () =
        if not (File.exists "./.tools/7za.exe") then
            let zipFileUrl = "http://www.7-zip.org/a/7za920.zip"
            let zipFileName = "./.tools/7za920.zip"
            "./.tools" |> Directory.create
            zipFileUrl |> File.download zipFileName
            zipFileName |> Zip.unzip "./.tools/"

    let private ensureLZ4 () =
        if not (File.exists "./.tools/lz4.exe") then
            let zipFileUrl = "https://github.com/lz4/lz4/releases/download/v1.8.1.2/lz4_v1_8_1_win64.zip"
            let zipFile = "./.tools/lz4-1.8.1-win64.zip"
            "./.tools" |> Directory.create
            zipFileUrl |> File.download zipFile
            zipFile |> Zip.unzip "./.tools/lz4"
            "./.tools/lz4/lz4.exe" |> File.copy "./.tools/lz4.exe"
            "./.tools/lz4" |> Directory.delete

    let private restoreCorpusFile fn =
        let dataFile = $"./.corpus/%s{fn}"
        if not (File.exists dataFile) then
            let corpusUrl = $"https://github.com/MiloszKrajewski/SilesiaCorpus/blob/master/%s{fn}.zip?raw=true"
            dataFile |> Path.dirnameOf |> Directory.create
            let zipFile = $"%s{dataFile}.zip"
            corpusUrl |> File.download zipFile
            Shell.run ".\\.tools\\7za.exe" $"-o%s{Path.dirnameOf zipFile} x %s{zipFile}"
            zipFile |> File.delete
        else
            Log.info $"Corpus file %s{dataFile} already exists"

    let restoreCorpus () =
        ensure7zip ()
        ensureLZ4 ()
        restoreCorpusFile "dickens"
        restoreCorpusFile "mozilla"
        restoreCorpusFile "mr"
        restoreCorpusFile "nci"
        restoreCorpusFile "ooffice"
        restoreCorpusFile "osdb"
        restoreCorpusFile "reymont"
        restoreCorpusFile "samba"
        restoreCorpusFile "sao"
        restoreCorpusFile "webster"
        restoreCorpusFile "xml"
        restoreCorpusFile "x-ray"
