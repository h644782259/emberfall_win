// Production-size contact sheet for art review; no source artwork is modified.
import AppKit
let root = URL(fileURLWithPath: CommandLine.arguments[1])
let output = URL(fileURLWithPath: CommandLine.arguments[2])
let width = 1040, height = 900
let rep = NSBitmapImageRep(bitmapDataPlanes:nil,pixelsWide:width,pixelsHigh:height,bitsPerSample:8,samplesPerPixel:4,hasAlpha:true,isPlanar:false,colorSpaceName:.deviceRGB,bytesPerRow:width*4,bitsPerPixel:32)!
NSGraphicsContext.saveGraphicsState()
NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep:rep)
NSColor(calibratedRed:0.025,green:0.045,blue:0.065,alpha:1).setFill();NSRect(x:0,y:0,width:width,height:height).fill()
func label(_ text:String,_ x:CGFloat,_ y:CGFloat,_ size:CGFloat=14){(text as NSString).draw(at:NSPoint(x:x,y:y),withAttributes:[.font:NSFont.systemFont(ofSize:size),.foregroundColor:NSColor.white])}
func icon(_ key:String,_ x:CGFloat,_ y:CGFloat,_ size:CGFloat){NSImage(contentsOf:root.appendingPathComponent(key+".png"))!.draw(in:NSRect(x:x,y:y,width:size,height:size))}
label("Emberfall / ImageGen / actual UI sizes",24,862,20)
label("Class attacks / jump / shop / achievement (48px)",24,831)
for i in 0..<4 {icon("actions/"+String(format:"%02d",i),520+CGFloat(i*76),814,48)}
for i in 0..<2 {icon("toolbar/"+String(format:"%02d",i),840+CGFloat(i*76),814,48)}
label("Equipment series: Common / Rare / Epic / Legendary (48px)",24,751)
for family in 0..<3 {
 let key=["weapon","armor","relic"][family];let x=CGFloat(24+family*335)
 label(key.uppercased(),x,718)
 for hero in 0..<4 {for rarity in 0..<4 {
 let ix=x+CGFloat(rarity*72);let y=CGFloat(650-hero*68)
 let color=[NSColor.gray,.systemBlue,.systemPurple,.systemOrange][rarity]
 color.setStroke();NSBezierPath(rect:NSRect(x:ix-2,y:y-2,width:52,height:52)).stroke()
 icon("equipment-\(key)/"+String(format:"%02d",hero*4+rarity),ix,y,48)
 }}
}
label("Gem tiers / items / fashion (32px)",24,419)
for rank in 0..<4 { icon("gems/"+String(format:"%02d",rank),24+CGFloat(rank*56),382,32) }
for index in 0..<8 { icon("resources/"+String(format:"%02d",index),280+CGFloat(index*56),382,32) }
for rank in 0..<4 { icon("fashion/"+String(format:"%02d",rank),780+CGFloat(rank*56),382,32) }
label("Skill identities (32px / 48px / 64px)",24,363)
for hero in 0..<4 {
 let key=["vanguard","arcanist","ranger","summoner"][hero];let y=CGFloat(277-hero*65)
 label(key,24,y+23)
 for skill in 0..<10 {let size:CGFloat=skill<3 ? [32,48,64][skill] : 48;icon("skills-\(key)/"+String(format:"%02d",skill),140+CGFloat(skill*76),y,size)}
}
label("Cooldown color recovery: 0 / 25 / 50 / 75 / 100%",24,26)
for step in 0..<5 {
 let x=CGFloat(530+step*84);let rect=NSRect(x:x,y:12,width:56,height:56)
 icon("skills-arcanist/00-cooldown",x,12,56)
 NSGraphicsContext.saveGraphicsState();NSBezierPath(rect:NSRect(x:x,y:12,width:56,height:CGFloat(step)*14)).addClip();icon("skills-arcanist/00",x,12,56);NSGraphicsContext.restoreGraphicsState()
 if step==4 {NSColor.cyan.setStroke();NSBezierPath(ovalIn:rect.insetBy(dx:-2,dy:-2)).stroke()}
}
NSGraphicsContext.restoreGraphicsState()
try FileManager.default.createDirectory(at:output.deletingLastPathComponent(),withIntermediateDirectories:true)
try rep.representation(using:.png,properties:[:])!.write(to:output)
print(output.path)
