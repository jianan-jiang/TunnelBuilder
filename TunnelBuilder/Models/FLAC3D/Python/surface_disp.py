import csv
import itasca as it
import math
import xlrd
#it.command("zone gridpoint group 'Topo_gp' range group 'Top' ")

def export_disp(filename,orig_x=0,orig_y=0):
    with open(filename,'w') as csvfile:
        csvwriter = csv.writer(csvfile,delimiter=',',quotechar='|',quoting=csv.QUOTE_MINIMAL)
        csvwriter.writerow(["x (m)","y(m)","vertical displacement (mm)","horizontal displacement (mm)"])
        for gp in it.gridpoint.list():
            if gp.in_group("Top"):
                csvwriter.writerow([str(gp.pos_x()+orig_x),str(gp.pos_y()+orig_y),str(gp.disp_z()*1000),str(math.sqrt(gp.disp_x()**2+gp.disp_y()**2)*1000)])
        print('Surface Displacement Exported')

import win32com.client
import sys,os
import glob

def plot_surfer_grid(filename,color_map_reverse=False):
    Surfer = win32com.client.Dispatch("Surfer.Application")
    f = os.path.abspath(filename)
    fn= os.path.splitext(f)[0]
    Plot = Surfer.Documents.Add(1)
    PageSetup = Plot.PageSetup
    MapFrame = Plot.Shapes.AddContourMap("{0}.grd".format(fn))
    ContourMap = MapFrame.Overlays(1)
    ContourMap.FillContours = True
    ContourMap.FillForegroundColorMap.LoadFile(Surfer.Path + "\ColorScales\Rainbow.clr")
    if(color_map_reverse):
        ContourMap.FillForegroundColorMap.Reverse()
        
    DataMin = ContourMap.FillForegroundColorMap.DataMin
    DataMax = ContourMap.FillForegroundColorMap.DataMax
    ContourLevels = ContourMap.Levels
    ContourLevels.AutoGenerate(MinLevel=math.floor(DataMin),MaxLevel=math.ceil(DataMax),Interval=1)
    ContourMap.FillForegroundColorMap.SetDataLimits(math.floor(DataMin),math.ceil(DataMax))
    ContourMap.LabelTolerance = 10
    for i in range(ContourLevels.Count):
        Level=ContourLevels.Item(Index=i+1)
        Level.ShowLabel=True
    ContourMap.ApplyFillToLevels(FirstIndex=1, NumberToSet=1, NumberToSkip=0)
    ContourMap.ShowColorScale = True
    Plot.SaveAs(FileName="{0}.srf".format(fn))

def to_surfer_grid(filename):
    Surfer = win32com.client.Dispatch("Surfer.Application")
    f = os.path.abspath(filename)
    fn= os.path.splitext(f)[0]
    res = Surfer.GridData(DataFile = f, xCol = 1, yCol = 2, zCol = 3,  ShowReport = False)
    if(res):
        if (os.path.exists("{0}_vertical.grd".format(fn))):
            os.remove("{0}_vertical.grd".format(fn))
        os.replace("{0}.grd".format(fn),"{0}_vertical.grd".format(fn))
    Surfer.GridData(DataFile = f, xCol = 1, yCol = 2, zCol = 4,  ShowReport = False)
    if(res):
        if os.path.exists("{0}_horizontal.grd".format(fn)):
            os.remove("{0}_horizontal.grd".format(fn))
        os.replace("{0}.grd".format(fn),"{0}_horizontal.grd".format(fn))
        