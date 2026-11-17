// Draws the 1024×1024 app icon: a white letter card with a big "A" on honey yellow, and a bee.
// Uses only macOS's built-in drawing, so no image tools need installing.
// Usage: swift scripts/draw-app-icon.swift
// Writes the iPad asset catalog's AppIcon.png and the shared app's Assets/AppIcon.png
// (the Mac window icon, and the source for the Mac .icns built by scripts/bundle-mac.sh).
// The icon is full-bleed and square: iPadOS and macOS round the corners themselves.
import AppKit

let size: CGFloat = 1024
let repo = URL(fileURLWithPath: #filePath).deletingLastPathComponent().deletingLastPathComponent()
let outputPaths = [
    "src/SpellingBee.App.iOS/Assets.xcassets/AppIcon.appiconset/AppIcon.png",
    "src/SpellingBee.App/Assets/AppIcon.png",
]

func color(_ hex: UInt32, alpha: CGFloat = 1) -> NSColor {
    NSColor(srgbRed: CGFloat((hex >> 16) & 0xFF) / 255, green: CGFloat((hex >> 8) & 0xFF) / 255,
            blue: CGFloat(hex & 0xFF) / 255, alpha: alpha)
}

// The app's own colours (src/SpellingBee.App/Styles/Colors.axaml).
let beeYellow = color(0xFFC93C), beeAmber = color(0xE89B00), ink = color(0x1F2933)
let edge = color(0xD9D2C0), currentBlue = color(0x2F6FDB)

// The app's typeface, so the icon matches the screens.
let fontURL = repo.appendingPathComponent("src/SpellingBee.App/Assets/Fonts/Andika-Bold.ttf")
CTFontManagerRegisterFontsForURL(fontURL as CFURL, .process, nil)
let letterFont = NSFont(name: "Andika-Bold", size: 520) ?? NSFont.boldSystemFont(ofSize: 520)

let bitmap = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: Int(size), pixelsHigh: Int(size),
                              bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
                              colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0)!
NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: bitmap)

// Background: honey yellow, a little deeper at the bottom.
NSGradient(starting: beeYellow, ending: color(0xFFB21A))!
    .draw(in: NSRect(x: 0, y: 0, width: size, height: size), angle: -90)

// The letter card, like the answer boxes in the game, with a soft shadow.
let card = NSRect(x: 212, y: 130, width: 520, height: 620)
NSGraphicsContext.saveGraphicsState()
let shadow = NSShadow()
shadow.shadowColor = color(0x8A5A00, alpha: 0.35)
shadow.shadowOffset = NSSize(width: 0, height: -18)
shadow.shadowBlurRadius = 36
shadow.set()
NSColor.white.setFill()
NSBezierPath(roundedRect: card, xRadius: 96, yRadius: 96).fill()
NSGraphicsContext.restoreGraphicsState()
edge.setStroke()
let cardOutline = NSBezierPath(roundedRect: card.insetBy(dx: 9, dy: 9), xRadius: 88, yRadius: 88)
cardOutline.lineWidth = 18
cardOutline.stroke()

// The letter.
let letter = NSAttributedString(string: "A", attributes: [.font: letterFont, .foregroundColor: ink])
let letterSize = letter.size()
letter.draw(at: NSPoint(x: card.midX - letterSize.width / 2, y: card.midY - letterSize.height / 2 + 30))

// A blue line under the letter, like the "current" outline in the game.
currentBlue.setFill()
NSBezierPath(roundedRect: NSRect(x: card.midX - 120, y: card.minY + 64, width: 240, height: 26),
             xRadius: 13, yRadius: 13).fill()

// The bee, flying in at the top right of the card.
let bee = NSAffineTransform()
bee.translateX(by: 760, yBy: 790)
bee.rotate(byDegrees: -20)
bee.scale(by: 0.85)
NSGraphicsContext.saveGraphicsState()
bee.concat()

// Wings behind the body.
color(0xFFFFFF, alpha: 0.92).setFill()
ink.setStroke()
for wing in [NSRect(x: -95, y: 40, width: 110, height: 150), NSRect(x: -5, y: 50, width: 100, height: 135)] {
    let path = NSBezierPath(ovalIn: wing)
    path.lineWidth = 12
    path.fill()
    path.stroke()
}

// Body with stripes, clipped to the body's oval.
let body = NSBezierPath(ovalIn: NSRect(x: -150, y: -80, width: 300, height: 190))
NSGraphicsContext.saveGraphicsState()
body.addClip()
beeAmber.setFill()
body.fill()
ink.setFill()
for stripeX in [-60.0, 20.0, 100.0] {
    NSBezierPath(rect: NSRect(x: stripeX, y: -100, width: 42, height: 230)).fill()
}
NSGraphicsContext.restoreGraphicsState()
body.lineWidth = 14
ink.setStroke()
body.stroke()

// Face: an eye and a smile on the leading end.
ink.setFill()
NSBezierPath(ovalIn: NSRect(x: -112, y: 18, width: 30, height: 30)).fill()
let smile = NSBezierPath()
smile.appendArc(withCenter: NSPoint(x: -95, y: -10), radius: 26, startAngle: 200, endAngle: 300)
smile.lineWidth = 10
smile.lineCapStyle = .round
smile.stroke()

NSGraphicsContext.restoreGraphicsState()

let png = bitmap.representation(using: .png, properties: [:])!
for path in outputPaths {
    try! png.write(to: repo.appendingPathComponent(path))
    print("Drew \(path)")
}
