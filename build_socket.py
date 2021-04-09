import os
import shutil

if __name__ == '__main__':
    dir_name = 'Socket_build'
    if os.path.exists(dir_name):
        for root, dirs, files in os.walk(dir_name, topdown=False):
            for name in files:
                os.remove(os.path.join(root, name))
            for name in dirs:
                os.rmdir(os.path.join(root, name))
    else:
        os.mkdir(dir_name)
    client_dir = dir_name + r'\Client'
    os.mkdir(client_dir)
    shutil.copy(r'FileManager\bin\Debug\FileManager.exe', client_dir)
    shutil.copy(r'FileManager\bin\Debug\SocketLib.dll', client_dir)
    server_dir = dir_name + r'\Server'
    os.mkdir(server_dir)
    shutil.copy(r'ServerForm\bin\Debug\ServerForm.exe', server_dir)
    shutil.copy(r'ServerForm\bin\Debug\SocketLib.dll', server_dir)
