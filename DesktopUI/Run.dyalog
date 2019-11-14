 r←Run req;page;input;inputArea;outputArea;N
 ⍝ Puts a frontend on the involute function

 :If 0∊⍴req ⍝ if no HTTP request we are setting up
     page←⎕NEW Page
     page.Size←410 510

     page.Add _.Style'#num,#dir'('border-style' 'solid' 'margin' '0.5em' 'padding' '10px')
     page.Add _.title 'IUI - The Involution UI'

     input←('N'('type' 'range')('min' 1)('max' 20)('value' 1))page.New _.EditField''
     
     inputArea←page.New _.InputGrid'Size:'input
     (outputArea←page.New _.StackPanel).Add¨'#num' '#dir'{⍺ page.New _.pre ⍵}¨1 '→'
     outputArea.Horizontal←1
     (page.Add _.StackPanel).Add¨inputArea outputArea

     input.On'change'(1⊃⎕SI) ⍝ Call self to handle changes
     page.Run

 :Else
     :If (,83)≡⎕DR¨N←#.Strings.tonum req._value ⍝ single integer?
         r←⊃,/'#num' '#dir'Page.Replace¨0 1 involute¨N
     :Else
         r←Page.Execute _JSS.Alert'Please enter a single integer'
     :EndIf
 :EndIf
