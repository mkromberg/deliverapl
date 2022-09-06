from pynapl import APL

def print_grid(grid):
    width = len(str(len(grid) ** 2))
    print("\n".join(", ".join(f"{v:{width}}" for v in row) for row in grid))

def print_list(grid):
    for row in grid:
        print(row)

apl=APL.APL()
apl.eval("2 ⎕FIX 'file://C:\devt\deliverapl\Interactive\involute.aplf'")
print_grid(apl.fn("{⍵ ⍵⍴⍋+\(1↓2/⌽⍳⍵)/(¯1+2×⍵)⍴(+,-)1 ⍵}")(5))
print_list(apl.fn("involute")(1,6))