
using System.Collections.Generic;
public struct Board {
    public string ardwiinoName { get; }
    public string name { get; } 
    public string environment { get; } 
    public List<uint> productIDs { get; } 

    public Board(string ardwiinoName, string name, string environment, List<uint> productIDs)
    {
        this.ardwiinoName = ardwiinoName;
        this.name = name;
        this.environment = environment;
        this.productIDs = productIDs;
    }

    public static readonly Board Generic = new Board("generic", "Generic Serial Device", "generic", new List<uint>{});
    public static readonly Board OldArdwiino = 
        new Board("ardwiino", "(unable to import config, updating will erase any configuration)", "megaadk", new List<uint>{});
    public static readonly Board[] Boards = {
        new Board("uno-atmega16u2", "Arduino Uno", "uno-atmega16u2", new List<uint>{0x2FEF}),
        new Board("uno-at90usb82", "Arduino Uno", "uno-at90usb82", new List<uint>{0x2FF7}),
        new Board("uno", "Arduino Uno", "uno", new List<uint>{0x0043, 0x7523, 0x0001, 0xea60, 0x0243}),
        new Board("mini", "Arduino Pro Mini", "mini", new List<uint>{}),
        new Board("a-micro", "Arduino Micro in Bootloader Mode", "a-micro", new List<uint>{0x0037, 0x0237}),
        new Board("a-micro", "Arduino Micro", "a-micro", new List<uint>{0x8037, 0x8237}),
        new Board("micro", "Arduino Pro Micro", "micro", new List<uint>{0x9204}),
        new Board("micro", "Arduino Pro Micro", "micro", new List<uint>{0x9206}),
        new Board("leonardo", "Arduino Leonardo", "leonardo", new List<uint>{0x8036, 0x800c}),
        new Board("leonardo", "Arduino Micro / Pro Micro / Leonardo in Bootloader Mode", "leonardo", new List<uint>{0x0036}),
        new Board("micro", "Arduino Pro Micro in Bootloader Mode", "micro", new List<uint>{0x9203, 0x9207}),
        new Board("micro", "Arduino Pro Micro in Bootloader Mode", "micro", new List<uint>{0x9205}),
        new Board("mega2560-atmega16u2", "Arduino Mega 2560", "mega2560-atmega16u2", new List<uint>{0x2FEF}),
        new Board("mega2560-at90usb82", "Arduino Mega 2560", "mega2560-at90usb82", new List<uint>{0x2FF7}),
        new Board("mega2560", "Arduino Mega 2560", "mega2560", new List<uint>{0x0010, 0x0042}),
        new Board("megaadk-atmega16u2", "Arduino Mega ADK", "megaadk-atmega16u2", new List<uint>{0x2FEF}),
        new Board("megaadk-at90usb82", "Arduino Mega ADK", "megaadk-at90usb82", new List<uint>{0x2FF7}),
        new Board("megaadk", "Arduino Mega ADK", "megaadk", new List<uint>{0x003f, 0x0044})
    };

    public static Board findBoard(string ardwiinoName) {
        foreach (var board in Boards) {
            if (board.ardwiinoName == ardwiinoName) {
                return board;
            }
        }
        return Generic;
    }
}