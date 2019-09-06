from datetime import datetime, timedelta
from pytz import timezone
import pytz
import itasca as it
from os import listdir, mkdir
import os.path
from os.path import isfile, join

attachment_file_dir = './Attachments'
attachment_file_dir = os.path.abspath(attachment_file_dir)

sydney = timezone('Australia/Sydney')
loc_dt = sydney.localize(datetime.now())
fmt = '%Y-%m-%d %H:%M:%S %Z%z'
current_time = loc_dt.strftime(fmt)

it.command('program mail clear')
it.command("program mail add to 'jianan.jiang@psm.com.au'")
it.command("program mail subject [global.title+' has just finished']")
it.command("program mail body string [global.title +' on Model 3 has finished computing on "+current_time+"']")

attachment_files = [f for f in listdir(attachment_file_dir) if isfile(join(attachment_file_dir,f))]
for a in attachment_files:
    it.command("program mail add attachment '"+attachment_file_dir+"\\"+a+"'")

it.command('program mail send')
it.command('program mail clear')
