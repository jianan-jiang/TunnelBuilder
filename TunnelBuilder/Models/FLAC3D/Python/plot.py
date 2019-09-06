import itasca as it
from os import listdir, mkdir
import os.path
from os.path import isfile, isdir, join
import time
import xlrd

import sys
sys.path.append(os.path.abspath('./util_scripts'))
import imp
import surface_disp
imp.reload(surface_disp)

plot_file_dir = './Plot_Data'
run_file_dir = './'
plot_save_dir = './Figures'
surface_disp_dir = './surface_disp'

plot_file_dir = os.path.abspath(plot_file_dir)
run_file_dir = os.path.abspath(run_file_dir)
plot_save_dir = os.path.abspath(plot_save_dir)
surface_disp_dir = os.path.abspath(surface_disp_dir)

if not isdir(plot_file_dir):
    raise Exception("Plot file directory doesn't exist")
if not isdir(run_file_dir):
    raise Exception("Run file directory doesn't exist")
if not isdir(plot_save_dir):
    mkdir(plot_save_dir)


size_x=3800
size_y=2217

it.command("python-reset-state false")
it.command("program warning false")

plot_files = [f for f in listdir(plot_file_dir) if isfile(join(plot_file_dir,f)) and os.path.splitext(f)[1]=='.dat']
run_files = [f for f in listdir(run_file_dir) if isfile(join(run_file_dir,f)) and os.path.splitext(f)[1]=='.f3sav' and os.path.splitext(f)[0][-1]=='7']

print('---RUN FILES---')
print(run_files)
print('---PLOT FILES---')
print(plot_files)

plot_description_workbook = xlrd.open_workbook("./util_scripts/Plot Description.xlsx")
plot_description_sheet = plot_description_workbook.sheet_by_name("Surface_displacement")
orig_x=0;
orig_y=0;
for k in range(0,plot_description_sheet.nrows):
    if (str(plot_description_sheet.cell(k,0).value)=="orig_x"):
        orig_x = float(plot_description_sheet.cell(k,1).value)
    if (str(plot_description_sheet.cell(k,0).value)=="orig_y"):
        orig_y = float(plot_description_sheet.cell(k,1).value)
    

for r in run_files:
    it.command("model restore '"+r+"'")
    run_name = os.path.splitext(r)[0]
    surface_disp.export_disp(surface_disp_dir+"/"+run_name+".csv")
    surface_disp.export_disp(surface_disp_dir+"/"+run_name+"_world_coordinates.csv",orig_x,orig_y)
    surface_disp.to_surfer_grid(surface_disp_dir+"/"+run_name+"_world_coordinates.csv")
    surface_disp.plot_surfer_grid("{0}_vertical.grd".format(surface_disp_dir+"/"+run_name+"_world_coordinates"),True)
    surface_disp.plot_surfer_grid("{0}_horizontal.grd".format(surface_disp_dir+"/"+run_name+"_world_coordinates"),False)
    for pf in plot_files:
        cmd = "call '"+plot_file_dir+"/"+pf+"'"
        plot_name = os.path.splitext(pf)[0]
        it.command(cmd)
        it.command("plot rename '"+plot_name+"'")
        it.command("plot export bitmap filename '"+plot_save_dir+"/"+run_name+"_"+plot_name+".png' size "+str(size_x)+" "+str(size_y))
        it.command("plot delete")




it.command("program warning true")