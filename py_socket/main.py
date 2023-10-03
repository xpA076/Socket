# This is a sample Python script.

# Press Shift+F10 to execute it or replace it with your code.
# Press Double Shift to search everywhere for classes, files, tool windows, actions, and settings.

import socket
import time

def print_hi(name):
    # Use a breakpoint in the code line below to debug your script.
    print(f'Hi, {name}')  # Press Ctrl+F8 to toggle the breakpoint.



def main():
    time.sleep(1)
    s = socket.socket()
    s.connect(('192.168.50.178', 12138))
    s.send(b'\x00\x00' +
           b'\x01\x30\x00\x00' +
           b'\x00\x00\x00\x00' +
           b'\x00\x00\x00\x00' +
           b'\x00\x00\x00\x00' +
           b'\x01\x00\x00\x00' +
           b'\x0A\x00\x00\x00' +
           b'\x0A\x00\x00\x00' +
           b'\x0A\x00\x00\x00' +
           b'music_next')
    print('ok')


# Press the green button in the gutter to run the script.
if __name__ == '__main__':
    main()


    # print_hi('PyCharm')

# See PyCharm help at https://www.jetbrains.com/help/pycharm/
