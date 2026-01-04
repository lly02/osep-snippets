import requests
import os
import uuid
import sys

def main():
    if len(sys.argv) <= 3:
        print("Usage: encode.py <url/local file> <type> <key> <-v>")
        print("type: xor, caesar")
        print("-v: output hex to console")
        sys.exit()
    
    path = sys.argv[1]
    url = False
    file = False if "-v" in sys.argv else True 
    encrypt = sys.argv[2]
    key = sys.argv[3]

    if "http" in path or "https" in path:
        url = True

    buf = ""
    if url:
        response = requests.get(path, False)
        buf = response.content
    else:
        with open(path, 'rb') as f:
            buf = f.read()

    encoded = ""
    if encrypt == "caesar":
        encoded = caesar(buf, key)
    elif encrypt == "xor":
        encoded = xor(buf, key)
    else:
        print("Encryption type not found.")
        sys.exit()

    if file:
        output = os.path.join(os.getcwd(), str(uuid.uuid4()))
        with open(output, "wb") as f:
            f.write(encoded)
            print(f"Written to {output}")
    else:
        output = ""
        for b in encoded:
            output += f'{"0x{:02x}".format(b)}, '
        print(output)

def caesar(buf, key):
    encoded = bytearray(len(buf))
    for index, b in enumerate(buf):
        encoded[index] = (b + ord(key)) & 0xFF
    return encoded

def xor(buf, key):
    encoded = bytearray(len(buf))
    for index, b in enumerate(buf):
        encoded[index] = (b ^ ord(key))
    return encoded

if __name__ == "__main__":
    main()
