# Introduction

A simple tool to bulk rename subtitles in a directory of video files.

## Example output

### Directory before bulk rename
```text
D:\> ls "Z:\shows\Prison Break\Season 01"


    Directory: Z:\shows\Prison Break\Season 01


Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
------         2025/8/18     19:52      710837214 Prison Break S01e01 Pilot.mkv
------         2025/8/18     19:52      808267103 Prison Break S01e02 Allen.mkv
------         2025/8/18     19:51      974238418 Prison Break S01e03 Cell Test.mkv
------         2025/8/19     19:05         217550 Prison.Break.S01E01.1080p.BluRay.x264-HALCYON.Chs&Eng.ass
------         2025/8/19     19:05         182570 Prison.Break.S01E02.1080p.BluRay.x264-HALCYON.Chs&Eng.ass
------         2025/8/19     19:05         200016 Prison.Break.S01E03.1080p.BluRay.x264-HALCYON.Chs&Eng.ass
```

### Command execution

```text
D:\> ./AutoSubName.exe --dir "Z:\shows\Prison Break" -r
Scanning subtitles in Z:\shows\Prison Break. This may take a while...
Renamed 3 subtitles.
```

### Directory after bulk rename

```text
D:\> ls "Z:\shows\Prison Break\Season 01"


    Directory: Z:\shows\Prison Break\Season 01


Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
------         2025/8/18     19:52      710837214 Prison Break S01e01 Pilot.mkv
------         2025/8/19     19:05         217550 Prison Break S01e01 Pilot.zh-Hans.ass
------         2025/8/18     19:52      808267103 Prison Break S01e02 Allen.mkv
------         2025/8/19     19:05         182570 Prison Break S01e02 Allen.zh-Hans.ass
------         2025/8/18     19:51      974238418 Prison Break S01e03 Cell Test.mkv
------         2025/8/19     19:05         200016 Prison Break S01e03 Cell Test.zh-Hans.ass
```

## Usage

### Bulk rename subtitles recursively
```text
./AutoSubName.exe --dir "/path/to/your/video-directory" -r
```

> ⚠ Warning: Don't put lots of films and subtitles into one directory since they can't be matched by season-episode. The matcher will try performing a sorting-order-based matching for these files, which may lead to unexpected behaviour in a large directory.

### Get help
```text
./AutoSubName.exe -h
```

```text
Description:
  Bulk rename subtitles in a directory of video files.

Usage:
  AutoSubName [options]

Options:
  -?, -h, --help                                       Show help and usage information
  --version                                            Show version information
  -d, --dir                                            The directory to scan for subtitles. Defaults to the current directory.
  -r, --recursive                                      Enable recursive directory scanning.
  -p, --pattern                                        Use a custom naming pattern. Defaults to "{name}{lang:.{}|}.{ext}".
                                                       Possible variables: {name}, {lang}, {ext}.
                                                       The format follows axuno/SmartFormat interpolation syntax.
                                                       See https://github.com/axuno/SmartFormat/wiki/How-Formatters-Work.
  -lf, --lang-format, --language-format                The output language format.
  <Display|English|Ietf|Native|ThreeLetter|TwoLetter>  TwoLetter: ISO 639-1 two-letter or ISO 639-3 three-letter code. e.g. "zh"
                                                       ThreeLetter: ISO 639-2 three-letter code. e.g. "zho"
                                                       Ietf: IETF BCP 47 language tag (RFC 4646). e.g. "zh-Hans"
                                                       English: language name in English.
                                                       Native: language name in the native language.
                                                       Display: language name in your system language. [default: Ietf]
  -v, --verbose                                        Enable verbose logging.
  -dr, --dry-run                                       Scan and output possible changes, but don't rename anything.
  -l, --languages                                      The subtitle languages to detect in ISO 639 or IETF BCP 47 format.
                                                       [default: en|zh-Hans|zh-Hant|ja|ko|es|fr|de|ru|pt|ar]
  -m, --matchers                                       Additional file name matchers to extract series information.
                                                       The default will match formats like S01E01 and XXXX-123.
                                                       A matcher is a regular expression. Match groups are extracted as series
                                                       information.
```

