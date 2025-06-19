$(document).ready(function() {

    $('#btn_getByClaim').on('click', function(e){

        e.preventDefault();
        
        $('#form_get_list').trigger("reset");

        if($('#claim_no').val() === ''){
            toastr['warning']("Field Claim No is Empty.")
            return ;
        }
        
        $('#claimsList').DataTable({
            "destroy": true,
            //"responsive": true, 
            "paging": true,
            "searching": true,
            "ordering": true,
            "processing": true,
            "serverSide": true,
            "columnDefs": [
                { className: "text-center", "targets": [ 7 ] },
                { "targets": [ 12 ], "visible": false, "searchable": false }
            ],
            "ajax": {
                headers: {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                },
                beforeSend: function(){
                    loaderShow('main_loader');
                },
                url: '/claims/getList',
                type: "POST",
                data: {
                    "claim_no" : $('#claim_no').val(),
                }
            },
            "initComplete": function( settings, json ) {
                loaderHide('main_loader');
            },
            "drawCallback": function(settings) {
                loaderHide('main_loader');
            },
            "createdRow": function( row, data, dataIndex){
                if( data[12] ==  1){
                    $(row).addClass('redClass');
                }
            }
        });
    });

    $('#btn_getList').on('click', function(e){
        e.preventDefault();

        $('#form_get_by_claim').trigger('reset');

        $('#claimsList').DataTable({
            "destroy": true,
            //"responsive": true, 
            "paging": true,
            "searching": true,
            "ordering": true,
            "processing": true,
            "serverSide": true,
            "columnDefs": [
                { className: "text-center", "targets": [ 7 ] },
                { "targets": [ 12 ], "visible": false, "searchable": false }
            ],
            "ajax": {
                "headers": {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                },
                "beforeSend": function(){
                    loaderShow('main_loader');
                },
                "url": '/claims/getList',
                "type": "POST",
                "data": {
                    "from_date" : $('#from_date').val(),
                    "to_date"   : $('#to_date').val(),
                    "type"      : $('#type').val(),
                    "card_no"   : $('#card_no').val(),
                }
            },
            "initComplete": function( settings, json ) {
                loaderHide('main_loader');
            },
            "drawCallback": function(settings) {
                loaderHide('main_loader');
            },
            "createdRow": function( row, data, dataIndex){
                if( data[12] ==  1){
                    $(row).addClass('redClass');
                }
            }
        });
    });

    $('#claimsList').on('click', '[data-preview]', function(){
        $.ajax({
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            type: "POST",
            url: '/claims/getscreenshots',
            data: {
                claimid : $(this).attr('data-preview'),
            },
            "beforeSend": function(){
                loaderShow('main_loader');
            },
            success: function(data){
                loaderHide('main_loader');
                $.magnificPopup.open({
                    items: data,
                    gallery: {
                        enabled: true
                    },
                    type: "image"
                }, 0);
            }
        });
    });

    $('#claimsList').on('click', '[data-download]', function(){
        $(this).parent().parent().parent().addClass('redClass');
    });

    $('#btn_export').on('click', function(e){
        e.preventDefault();
        $.ajax({
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            type: "GET",
            url: '/claims/export',
            data: $('#form_get_list').serialize(),
            xhrFields:{
                responseType: 'blob'
            },
            beforeSend: function(){
                loaderShow('main_loader');
            },
            success: function(response){
                
                var file_name = '';

                if($('#type').val() != '' || $('#type').val() != '0'){
                    file_name += $('#type :selected').html()+'-';
                }

                if($('#from_date').val() != ''){
                    file_name += 'From - '+$('#from_date').val()+'-';
                }

                if($('#to_date').val() != ''){
                    file_name += 'To - '+$('#to_date').val()+'-';
                }

                file_name += ' report';

                const blob = new Blob([response], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;' });
                const downloadUrl = URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = downloadUrl;
                a.download = file_name+".xlsx";
                document.body.appendChild(a);
                loaderHide('main_loader');
                a.click();
            },
            error : function(){
                loaderHide('main_loader');
                toastr["error"]("Some Error Occured");
            }
        });
    });

});