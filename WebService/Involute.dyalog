r←Involute N;iv
⍝ Prefix version for use in services

  (size char)←2↑N,1             ⍝ Default char to 1

  iv←{                          ⍝ Involute function
     ⍺←0                        ⍝ default: return numbers
     moves←(¯1+2×⍵)⍴m←1 ⍵,-1 ⍵  ⍝ sequence of 1 ⍵ ¯1 (-⍵)
     repeat←1↓2/⌽⍳⍵             ⍝ number of times to repeat each move
     path←+\repeat/moves        ⍝ path through matrix (viewed as a list)
     ⍺≡0:⍵ ⍵⍴⍋path              ⍝ return numbers if ⍺=0
     charmoves←repeat/'→↓←↑'[m⍳moves] ⍝ else map moves to arrows
     r←⍵ ⍵⍴(charmoves@path)(⍵×⍵)⍴⍺    ⍝ insert arrows along path
     ((2×⍵)⍴1 0)\r              ⍝ insert alternate blank columns
  }

  r←↓char iv size                  ⍝ Split into vector of char vectors