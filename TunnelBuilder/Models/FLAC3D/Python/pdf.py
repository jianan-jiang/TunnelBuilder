import itasca as it
from os import listdir
import os.path
from os.path import isfile, join
import os
import ctypes, sys
import datetime

def is_admin():
    try:
        return ctypes.windll.shell32.IsUserAnAdmin()
    except:
        return False

class UnableToInstallPackageError(Exception):
    def __init__(self,value):
        self.value = value
    def __str__(self):
        return repr(self.value)

def install_if_not_exist(package):
    import imp
    try:
        imp.find_module(package)
        print("{0} not found, trying to install...".format(package))
        found = True
    except ImportError:
        found = False

    if (found == False):
        if(is_admin()):
            import subprocess as Popen
            import subprocess as sp
            import site; 
            path = site.getsitepackages()[0]
            cmd = '{0} install {1}'.format(path+"/Scripts/pip.exe",package)
            print(cmd)
            r = subprocess.check_output('cmd.exe /K {0}'.format(cmd),shell=False) 
            print (r)
            print("{0} has been installed".format(package))
        else:
            print("Unable to install {0} without admin access, run the script after opening FLAC3D with admin privilege".format(package))
            if(sys.version_info[0]<3):
                ctypes.windll.shell32.ShellExecuteW(None, u"runas", unicode(sys.executable), "", None, 1)
            else:
                ctypes.windll.shell32.ShellExecuteW(None,"runas",sys.executable,"",None,1)
            raise UnableToInstallPackageError("Unable to install {0} without admin access, run the script after opening FLAC3D with admin privilege".format(package))


# Install packages if it is not installed

install_if_not_exist('fpdf')
install_if_not_exist('xlrd')

from fpdf import FPDF
import xlrd

image_save_dir = './Figures'
pdf_save_dir = './Attachments'
run_file_dir = './'

image_save_dir = os.path.abspath(image_save_dir)
run_file_dir = os.path.abspath(run_file_dir)
pdf_save_dir  = os.path.abspath(pdf_save_dir)

run_files = []

plot_description_workbook = xlrd.open_workbook("./util_scripts/Plot Description.xlsx")
plot_description_sheet = plot_description_workbook.sheet_by_name("Plots")
run_description_sheet = plot_description_workbook.sheet_by_name("Runs")
plot_description = {}
run_description={}
attachment_ref={}
for k in range(1,plot_description_sheet.nrows):
    plot_description[str(plot_description_sheet.cell(k,0).value)]=str(plot_description_sheet.cell(k,1).value)
for k in range(1,run_description_sheet.nrows):
    run_files.append(run_description_sheet.cell(k,0).value)
    run_description[str(run_description_sheet.cell(k,0).value)]=str(run_description_sheet.cell(k,1).value)
    attachment_ref[str(run_description_sheet.cell(k,0).value)]=str(run_description_sheet.cell(k,2).value)

#Preamble
CLIENT='JHCPB JV'
DESCRIPTION_LINE1='Rozelle Interchange'
DESCRIPTION_LINE2='M4 To WHT'

DESCRIPTION_LINE4=plot_description

DOC_REF="PSM3696-161M"


def exportImageFile(image_files,TITLE,ATTACHMENT_REF,DESCRIPTION_LINE3):
    print('---EXPORT PDF FOR {0}---'.format(TITLE.upper()))
    pdf = FPDF(orientation='L', unit='cm', format='A3')
    pdf.add_font('checkbook', '', fname=r'./util_scripts/CHECKBK0.TTF', uni=True)
    pdf.set_author("Pells Sullivan Meynink")
    pdf.set_creator("FLAC3D")
    pdf.set_display_mode("fullpage",layout="single")
    pdf.set_title(TITLE)
    pdf.set_subject(TITLE)
    attachment_number = 1;
    FILE_VERSION="{0}".format(datetime.datetime.now().strftime("%d/%m/%Y"));
    for name,description in plot_description.items():
        pdf.add_page()
        pdf.set_margins(0,0,0)
        pdf.set_auto_page_break(False)
        #Draw Borders
        pdf.line(1.3,1,41,1)
        pdf.line(1.3,1,1.3,28.2)
        pdf.line(1.3,28.2,41,28.2)
        pdf.line(41,1,41,28.2)
        #Insert Image
        pdf.image(image_save_dir+"/"+image_files[name],x=2.5,y=1.8,w=37.1,h=22)
        #Insert Logo
        pdf.image("./util_scripts/PSM Logo.jpg",x=22,y=26.6,w=2.02,h=1.38,link='www.psm.com.au')
        pdf.set_font('Arial','B',12.5)
        pdf.text(x=24.5,y=27.9,txt="Pells Sullivan Meynink")
        pdf.set_font_size(10)
        pdf.set_y(24.2)
        pdf.set_x(31.5)
        pdf.cell(w=9.5,h=0.64,txt=CLIENT,border='LT',ln=2,align='C')
        pdf.cell(w=9.5,h=0.64,txt=DESCRIPTION_LINE1,border='L',ln=2,align='C')
        pdf.cell(w=9.5,h=0.64,txt=DESCRIPTION_LINE2,border='L',ln=2,align='C')
        pdf.set_font_size(7.5)
        pdf.cell(w=9.5,h=0.64,txt=DESCRIPTION_LINE3,border='L',ln=2,align='C')
        pdf.set_font_size(7)
        pdf.cell(w=9.5,h=0.64,txt=description,border='LB',ln=2,align='C')
        pdf.set_font_size(10)
        pdf.cell(w=4.75,h=0.8,txt=DOC_REF,border='LR',align='L',ln=0)
        pdf.cell(w=0,h=0.8,txt=ATTACHMENT_REF+str(attachment_number),align='L',ln=1)
        
        #Footer
        pdf.set_font('Arial','I',8)
    
        pdf.set_y(28.2)
        pdf.set_x(1.3)
        pdf.cell(0,1,txt=os.path.abspath(image_files[name]))
        
        pdf.set_font('Checkbook','',8)
        pdf.set_y(28.2)
        pdf.set_x(-pdf.get_string_width(FILE_VERSION)-1.3)
        pdf.cell(0,1,txt=FILE_VERSION,align='L')
    
        attachment_number = attachment_number+1
    pdf.output(pdf_save_dir+'/{0}.pdf'.format(TITLE))
    print('---DONE---')

for r in run_files:
    run_name = os.path.splitext(r)[0]
    image_locations = [f for f in listdir(image_save_dir) if isfile(join(image_save_dir,f)) and os.path.splitext(f)[1]=='.png' and f.startswith(run_name)]
    image_files = {}
    for i in image_locations:
        image_files[os.path.splitext(i)[0].split('_')[-1]]=i
    exportImageFile(image_files,run_name,attachment_ref[run_name],run_description[run_name])


